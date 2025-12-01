using OpenNEL.Network;
using OpenNEL.Entities;
using OpenNEL.type;
using OpenNEL.Manager;
using System.Text.Json;
using Codexus.Cipher.Entities;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;
using Codexus.Cipher.Protocol;
using Codexus.Game.Launcher.Services.Java;
using Codexus.Game.Launcher.Utils;
using Codexus.Interceptors;
using Codexus.OpenSDK;
using OpenNEL.Entities.Web.NetGame;
using OpenNEL.Utils;
using Serilog;

namespace OpenNEL.Message.Game;

internal class JoinGameMessage : IWsMessage
{
    private EntityJoinGame? _request;
    public string Type => "join_game";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        _request = JsonSerializer.Deserialize<EntityJoinGame>(root);
        var serverId = _request.ServerId;
        var serverName = _request.ServerName;
        var role = _request.Role;
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null) return new { type = "notlogin" };
        if (string.IsNullOrWhiteSpace(serverId) || string.IsNullOrWhiteSpace(role))
        {
            return new { type = "start_error", message = "参数错误" };
        }
        try
        {
            var ok = await StartAsync(serverId!, serverName, role!);
            if (!ok) return new { type = "start_error", message = "启动失败" };
            return new { type = "channels_updated" };
        }
        catch (System.Exception ex)
        {
            Serilog.Log.Error(ex, "启动失败");
            return new { type = "start_error", message = "启动失败" };
        }
    }

    public async Task<bool> StartAsync(string serverId, string serverName, string roleId)
    {
        var available = UserManager.Instance.GetLastAvailableUser();
        if (available == null) return false;
        var entityId = available.UserId;
        var token = available.AccessToken;
        var auth = new Codexus.OpenSDK.Entities.X19.X19AuthenticationOtp { EntityId = entityId, Token = token };

        var roles = await auth.Api<EntityQueryGameCharacters, Entities<EntityGameCharacter>>(
            "/game-character/query/user-game-characters",
            new EntityQueryGameCharacters { GameId = serverId, UserId = entityId });
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
            entityId,
            available.AccessToken,
            gameVersion,
            new WPFLauncher(),
            serverId,
            false);
        var mods = JsonSerializer.Serialize(serverModInfo);
        SemaphoreSlim authorizedSignal = new SemaphoreSlim(0);
        var pair = Md5Mapping.GetMd5FromGameVersion(version.Name);

        Interceptor interceptor = Interceptor.CreateInterceptor(_request.Socks5, mods, serverId, serverName, version.Name, address.Data!.Ip, address.Data!.Port, _request.Role, available.UserId, available.AccessToken, delegate(string certification)
        {
            Log.Logger.Information("Server certification: {Certification}", certification);
            Task.Run(async delegate
            {
                try
                {
                    var latest = UserManager.Instance.GetAvailableUser(entityId);
                    var currentToken = latest?.AccessToken ?? token;
                    var success = await AppState.Services!.Yggdrasil.JoinServerAsync(new Codexus.OpenSDK.Entities.Yggdrasil.GameProfile
                    {
                        GameId = serverId,
                        GameVersion = version.Name,
                        BootstrapMd5 = pair.BootstrapMd5,
                        DatFileMd5 = pair.DatFileMd5,
                        Mods = JsonSerializer.Deserialize<Codexus.OpenSDK.Entities.Yggdrasil.ModList>(mods)!,
                        User = new Codexus.OpenSDK.Entities.Yggdrasil.UserProfile { UserId = int.Parse(entityId), UserToken = currentToken }
                    }, certification);
                    if (success.IsSuccess) if (AppState.Debug) Log.Information("消息认证成功");
                        else
                        {
                            if (AppState.Debug)Log.Error(new Exception(success.Error ?? "未知错误"), "消息认证失败，详细信息: {Error}", success.Error);
                            else Log.Error("消息认证失败: {Error}", success.Error);
                        }
                }
                catch (Exception e)
                {
                    Log.Error(e, "认证过程中发生异常");
                }
                finally
                {
                    authorizedSignal.Release();
                }
            });
            authorizedSignal.Wait();
        });
        InterConn.GameStart(available.UserId, available.AccessToken, _request.GameId).GetAwaiter().GetResult();
        GameManager.Instance.AddInterceptor(interceptor);

        await X19.InterconnectionApi.GameStartAsync(entityId, available.AccessToken, serverId);
        return true;
    }
}
