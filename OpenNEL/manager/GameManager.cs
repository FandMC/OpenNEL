using System.Text.Json;
using Codexus.Cipher.Entities;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;
using Codexus.Cipher.Protocol;
using Codexus.Development.SDK.Entities;
using Codexus.Development.SDK.RakNet;
using Codexus.Game.Launcher.Services.Java;
using Codexus.Game.Launcher.Utils;
using Codexus.OpenSDK;
using Codexus.Interceptors;
using OpenNEL.Entities.Web.NEL;
using OpenNEL.type;
using OpenNEL.Utils;
using Serilog;

namespace OpenNEL.Manager;

internal class GameManager
{
    private readonly Lock _lock = new Lock();
    static readonly Dictionary<Guid, Codexus.Game.Launcher.Services.Java.LauncherService> Launchers = new();
    static readonly Dictionary<Guid, Codexus.Game.Launcher.Services.Bedrock.LauncherService> PeLaunchers = new();
    static readonly Dictionary<Guid, Interceptor> Interceptors = new();
    static readonly Dictionary<Guid, IRakNet> PeInterceptors = new();
    static readonly object Lock = new object();
    public static GameManager Instance { get; } = new GameManager();

    public sealed class LockScope : IDisposable
    {
        readonly object l;
        public LockScope(object o){l=o; Monitor.Enter(l);} 
        public void Dispose(){ Monitor.Exit(l);} 
    }
    public static LockScope EnterScope(object o)=>new LockScope(o);

    public async Task<bool> StartAsync(string serverId, string serverName, string roleId)
    {
        var sel = AppState.SelectedAccountId;
        if (string.IsNullOrEmpty(sel) || !AppState.Auths.TryGetValue(sel, out var auth)) return false;

        var roles = await auth.Api<EntityQueryGameCharacters, Entities<EntityGameCharacter>>(
            "/game-character/query/user-game-characters",
            new EntityQueryGameCharacters { GameId = serverId, UserId = auth.EntityId });
        var selected = roles.Data.FirstOrDefault(r => r.Name == roleId);
        if (selected == null) return false;

        var details = await auth.Api<EntityQueryNetGameDetailRequest, Entity<EntityQueryNetGameDetailItem>>(
            "/item-details/get_v2",
            new EntityQueryNetGameDetailRequest { ItemId = serverId });

        var address = await auth.Api<EntityAddressRequest, Entity<EntityNetGameServerAddress>>(
            "/item-address/get",
            new EntityAddressRequest { ItemId = serverId });

        var version = details.Data!.McVersionList[0];
        var gameVersion = GameVersionUtil.GetEnumFromGameVersion(version.Name);

        var serverModInfo = await InstallerService.InstallGameMods(
            auth.EntityId,
            auth.Token,
            gameVersion,
            new WPFLauncher(),
            serverId,
            false);

        var mods = JsonSerializer.Serialize(serverModInfo);

        var cts = new CancellationTokenSource();
        var connection = Interceptor.CreateInterceptor(
            new EntitySocks5 { Enabled = false },
            mods,
            serverId,
            serverName,
            version.Name,
            address.Data!.Ip,
            address.Data!.Port,
            selected.Name,
            auth.EntityId,
            auth.Token,
            (Action<string>)((sid) =>
            {
                var pair = Md5Mapping.GetMd5FromGameVersion(version.Name);
                var signal = new SemaphoreSlim(0);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var success = await AppState.Services!.Yggdrasil.JoinServerAsync(new Codexus.OpenSDK.Entities.Yggdrasil.GameProfile
                        {
                            GameId = serverId,
                            GameVersion = version.Name,
                            BootstrapMd5 = pair.BootstrapMd5,
                            DatFileMd5 = pair.DatFileMd5,
                            Mods = JsonSerializer.Deserialize<Codexus.OpenSDK.Entities.Yggdrasil.ModList>(mods)!,
                            User = new Codexus.OpenSDK.Entities.Yggdrasil.UserProfile { UserId = int.Parse(auth.EntityId), UserToken = auth.Token }
                        }, sid);
                        if (success.IsSuccess)
                        {
                            if (AppState.Debug) Log.Information("消息认证成功");
                        }
                        else
                        {
                            if (AppState.Debug)
                            {
                                Log.Error(new Exception(success.Error ?? "未知错误"), "消息认证失败，详细信息: {Error}", success.Error);
                            }
                            else
                            {
                                Log.Error("消息认证失败: {Error}", success.Error);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "认证过程中发生异常");
                    }
                    finally
                    {
                        signal.Release();
                    }
                });
                signal.Wait();
            })
        );

        var identifier = Guid.NewGuid();
        using (EnterScope(Lock))
        {
            Interceptors[identifier] = connection;
        }
        AppState.Channels[serverId] = new ChannelInfo
        {
            ServerId = serverId,
            ServerName = serverName,
            Ip = address.Data!.Ip,
            Port = address.Data!.Port,
            RoleName = selected.Name,
            Cts = cts,
            PlayerId = auth.EntityId,
            ForwardHost = address.Data!.Ip,
            ForwardPort = address.Data!.Port,
            LocalPort = address.Data!.Port,
            Connection = connection,
            Identifier = identifier
        };

        await X19.InterconnectionApi.GameStartAsync(auth.EntityId, auth.Token, serverId);
        return true;
    }
    
    public List<EntityQueryInterceptors> GetQueryInterceptors()
    {
        return Interceptors.Values.Select((Interceptor interceptor, int index) => new EntityQueryInterceptors
        {
            Id = index.ToString(),
            Name = interceptor.Identifier,
            Address = $"{interceptor.ForwardAddress}:{interceptor.ForwardPort}",
            Role = interceptor.NickName,
            Server = interceptor.ServerName,
            Version = interceptor.ServerVersion,
            LocalAddress = $"{interceptor.LocalAddress}:{interceptor.LocalPort}"
        }).ToList();
    }
    
    public void ShutdownInterceptor(Guid identifier)
    {
        Interceptor value = null;
        var has = false;
        using (EnterScope(Lock))
        {
            if (Interceptors.TryGetValue(identifier, out value))
            {
                Interceptors.Remove(identifier);
                has = true;
            }
        }
        if (has)
        {
            value.ShutdownAsync();
        }
    }
    public void AddInterceptor(Interceptor interceptor)
    {
        using (_lock.EnterScope())
        {
            Interceptors.Add(interceptor.Identifier, interceptor);
        }
    }
}
