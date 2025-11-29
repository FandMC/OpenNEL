using OpenNEL.Network;
using OpenNEL.Manager;
using System.Text.Json;
using Codexus.Cipher.Entities;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;
using Codexus.Cipher.Entities.WPFLauncher.RentalGame;
using Codexus.Development.SDK.Entities;
using Codexus.Game.Launcher.Services.Java;
using Codexus.Game.Launcher.Utils;
using Codexus.Development.SDK.RakNet;
using Serilog;
using OpenNEL.type;
using OpenNEL.Utils;

namespace OpenNEL.Message.Game;

internal class StartRentalProxyMessage : IWsMessage
{
    public string Type => "start_rental_proxy";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var serverId = root.TryGetProperty("serverId", out var sid) ? sid.GetString() : null;
        var nickname = root.TryGetProperty("role", out var rn) ? rn.GetString() : "Player";
        var password = root.TryGetProperty("password", out var pw) ? pw.GetString() : string.Empty;
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null) return new { type = "notlogin" };
        if (string.IsNullOrWhiteSpace(serverId)) return new { type = "start_error", message = "参数错误" };
        var auth = new Codexus.OpenSDK.Entities.X19.X19AuthenticationOtp { EntityId = last.UserId, Token = last.AccessToken };
        try
        {
            Entity<EntityRentalGameDetails>? details = null;
            Entity<EntityRentalGameServerAddress>? addrRental = null;
            try
            {
                details = await auth.Api<EntityQueryRentalGameDetail, Entity<EntityRentalGameDetails>>(
                    "/rental-server/query/server-detail",
                    new EntityQueryRentalGameDetail { ServerId = serverId! });
            }
            catch { }
            try
            {
                addrRental = await auth.Api<EntityQueryRentalGameServerAddress, Entity<EntityRentalGameServerAddress>>(
                    "/rental-server/query/server-address",
                    new EntityQueryRentalGameServerAddress { ServerId = serverId!, Password = password ?? string.Empty });
            }
            catch { }

            var versionName = details?.Data?.McVersion ?? string.Empty;
            EntityNetGameServerAddress? resolvedAddr = null;
            if (!string.IsNullOrWhiteSpace(details?.Data?.ServerIp) && details?.Data?.ServerPort is not null)
            {
                resolvedAddr = new EntityNetGameServerAddress { Ip = details!.Data!.ServerIp!, Port = details!.Data!.ServerPort!.Value };
            }
            else if (addrRental?.Data != null)
            {
                resolvedAddr = new EntityNetGameServerAddress { Ip = addrRental.Data.McServerHost, Port = addrRental.Data.McServerPort };
            }
            if (resolvedAddr == null)
            {
                try
                {
                    var netDetails = await auth.Api<EntityQueryNetGameDetailRequest, Entity<EntityQueryNetGameDetailItem>>(
                        "/item-details/get_v2",
                        new EntityQueryNetGameDetailRequest { ItemId = serverId! });
                    var netAddress = await auth.Api<EntityAddressRequest, Entity<EntityNetGameServerAddress>>(
                        "/item-address/get",
                        new EntityAddressRequest { ItemId = serverId! });
                    if (netDetails.Data != null && netAddress.Data != null)
                    {
                        versionName = netDetails.Data.McVersionList != null && netDetails.Data.McVersionList.Length > 0
                            ? netDetails.Data.McVersionList[0].Name
                            : versionName;
                        resolvedAddr = netAddress.Data;
                    }
                }
                catch { }
            }
            if (resolvedAddr == null)
            {
                return new { type = "start_error", message = "无法获取服务器地址" };
            }

            var gameVersion = GameVersionUtil.GetEnumFromGameVersion(string.IsNullOrWhiteSpace(versionName) ? "1.20.1" : versionName);
            var serverModInfo = await InstallerService.InstallGameMods(
                auth.EntityId,
                auth.Token,
                gameVersion,
                new Codexus.Cipher.Protocol.WPFLauncher(),
                serverId!,
                false);
            var mods = JsonSerializer.Serialize(serverModInfo);

            var connection = Codexus.Interceptors.Interceptor.CreateInterceptor(
                new EntitySocks5 { Enabled = false },
                mods,
                serverId!,
                details?.Data?.Name ?? serverId!,
                string.IsNullOrWhiteSpace(versionName) ? "" : versionName,
                resolvedAddr.Ip,
                resolvedAddr.Port,
                nickname ?? "Player",
                auth.EntityId,
                auth.Token,
                (sid2) =>
                {
                    var pair = Md5Mapping.GetMd5FromGameVersion(string.IsNullOrWhiteSpace(versionName) ? "" : versionName);
                    var signal = new System.Threading.SemaphoreSlim(0);
                    _ = System.Threading.Tasks.Task.Run(async () =>
                    {
                        try
                        {
                            var success = await AppState.Services!.Yggdrasil.JoinServerAsync(new Codexus.OpenSDK.Entities.Yggdrasil.GameProfile
                            {
                                GameId = serverId!,
                                GameVersion = string.IsNullOrWhiteSpace(versionName) ? "" : versionName,
                                BootstrapMd5 = pair.BootstrapMd5,
                                DatFileMd5 = pair.DatFileMd5,
                                Mods = JsonSerializer.Deserialize<Codexus.OpenSDK.Entities.Yggdrasil.ModList>(mods)!,
                                User = new Codexus.OpenSDK.Entities.Yggdrasil.UserProfile { UserId = int.Parse(auth.EntityId), UserToken = auth.Token }
                            }, sid2);
                            if (!success.IsSuccess)
                            {
                                Log.Warning("Yggdrasil 认证失败: {Error}", success.Error);
                            }
                        }
                        catch { }
                        finally
                        {
                            signal.Release();
                        }
                    });
                    signal.Wait();
                }
            );

            await Codexus.OpenSDK.X19.InterconnectionApi.GameStartAsync(auth.EntityId, auth.Token, serverId!);
            return new { type = "channels_updated" };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "启动租赁服失败: {ServerId}", serverId);
            return new { type = "start_error", message = ex.Message ?? "启动失败" };
        }
    }
}
