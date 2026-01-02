/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using System;
using System.Linq;
using OpenNEL_WinUI.Entities;
using OpenNEL_WinUI.type;
using OpenNEL_WinUI.Manager;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using Codexus.Cipher.Protocol;
using Codexus.Game.Launcher.Services.Java;
using Codexus.Game.Launcher.Utils;
using Codexus.Interceptors;
using Codexus.OpenSDK;
using Codexus.Development.SDK.Entities;
using OpenNEL_WinUI.Entities.Web.NetGame;
using Codexus.Game.Launcher.Entities;
using OpenNEL.Core.Utils;
using Serilog;

namespace OpenNEL_WinUI.Handlers.Game.NetServer;

public class JoinGame
{
    private EntityJoinGame? _request;
    private string _lastIp = string.Empty;
    private int _lastPort;

    public async Task<JoinGameResult> Execute(string accountId, string serverId, string serverName, string roleId)
    {
        new SelectAccount().Execute(accountId);
        var req = BuildRequest(serverId, serverName, roleId);
        return await Execute(req);
    }

    static EntityJoinGame BuildRequest(string serverId, string serverName, string roleId)
    {
        var req = new EntityJoinGame { ServerId = serverId, ServerName = serverName, Role = roleId, GameId = serverId };
        var set = SettingManager.Instance.Get();
        var enabled = set?.Socks5Enabled ?? false;
        req.Socks5 = (!enabled || string.IsNullOrWhiteSpace(set?.Socks5Address))
            ? new EntitySocks5 { Address = string.Empty, Port = 0, Username = string.Empty, Password = string.Empty }
            : new EntitySocks5 { Address = set!.Socks5Address, Port = set.Socks5Port, Username = set.Socks5Username, Password = set.Socks5Password };
        return req;
    }

    public async Task<JoinGameResult> Execute(EntityJoinGame request)
    {
        _request = request;
        var serverId = _request.ServerId;
        var serverName = _request.ServerName;
        var role = _request.Role;
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null) return new JoinGameResult { NotLogin = true };
        if (string.IsNullOrWhiteSpace(serverId) || string.IsNullOrWhiteSpace(role))
        {
            return new JoinGameResult { Success = false, Message = "参数错误" };
        }
        try
        {
            var ok = await StartAsync(serverId!, serverName, role!);
            if (!ok) return new JoinGameResult { Success = false, Message = "启动失败" };
            return new JoinGameResult { Success = true, Ip = _lastIp, Port = _lastPort };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "启动失败");
            return new JoinGameResult { Success = false, Message = "启动失败" };
        }
    }

    public async Task<bool> StartAsync(string serverId, string serverName, string roleId)
    {
        var available = UserManager.Instance.GetLastAvailableUser();
        if (available == null) return false;
        var roles = AppState.X19.QueryNetGameCharacters(available.UserId, available.AccessToken, serverId);
        var selected = roles.Data.FirstOrDefault(r => r.Name == roleId);
        if (selected == null) return false;
        var details = AppState.X19.QueryNetGameDetailById(available.UserId, available.AccessToken, serverId);
        var address = AppState.X19.GetNetGameServerAddress(available.UserId, available.AccessToken, serverId);
        
        var serverIp = address.Data!.Ip;
        var serverPort = address.Data!.Port;
        if (serverPort <= 0 && details.Data != null)
        {
            if (!string.IsNullOrWhiteSpace(details.Data.ServerAddress) && details.Data.ServerPort > 0)
            {
                serverIp = details.Data.ServerAddress;
                serverPort = details.Data.ServerPort;
            }
        }
        
        if (serverPort <= 0)
        {
            Log.Warning("服务器端口为 0，强制使用默认端口 25565");
            serverPort = 25565;
        }
        
        var version = details.Data!.McVersionList[0];
        var gameVersion = GameVersionUtil.GetEnumFromGameVersion(version.Name);
        var serverMod = await InstallerService.InstallGameMods(
            available.UserId,
            available.AccessToken,
            gameVersion,
            new WPFLauncher(),
            serverId,
            false);
        var mods = JsonSerializer.Serialize(serverMod);
        SemaphoreSlim authorizedSignal = new SemaphoreSlim(0);
        var pair = Md5Mapping.GetMd5FromGameVersion(version.Name);

        _lastIp = serverIp;
        _lastPort = serverPort;
        var socksCfg = _request.Socks5;
        var socksAddr = socksCfg != null ? (socksCfg.Address ?? string.Empty) : string.Empty;
        var socksPort = socksCfg != null ? socksCfg.Port : 0;
        Log.Information("JoinGame 接收的 SOCKS5 配置: Address={Addr}, Port={Port}, Username={User}, Enabled={Enabled}", socksAddr, socksPort, socksCfg?.Username, !string.IsNullOrWhiteSpace(socksAddr) && socksPort > 0);
        if (!string.IsNullOrWhiteSpace(socksAddr) && socksPort <= 0) return false;
        if (!string.IsNullOrWhiteSpace(socksAddr) && socksPort > 0)
        {
            try { Dns.GetHostAddresses(socksAddr); }
            catch { return false; }
        }
        Interceptor interceptor = Interceptor.CreateInterceptor(_request.Socks5, mods, serverId, serverName, version.Name, serverIp, serverPort, _request.Role, available.UserId, available.AccessToken, delegate(string certification)
        {
            Log.Information("SOCKS5 => Host: {Host}, Port: {Port}, User: {User} pass: {Pass}",
                _request.Socks5.Address,
                _request.Socks5.Port,
                _request.Socks5.Username,
                _request.Socks5.Password);
            Log.Logger.Information("Server certification: {Certification}", certification);
                    Task.Run(async delegate
                    {
                        try
                        {
                            var salt = CrcSalt.GetCached();
                            Log.Information("加入游戏 CrcSalt: {Salt}", salt);
                            AppState.Services?.RefreshYggdrasil();
                            var latest = UserManager.Instance.GetAvailableUser(available.UserId);
                            var currentToken = latest?.AccessToken ?? available.AccessToken;
                            var success = await AppState.Services!.Yggdrasil.JoinServerAsync(new Codexus.OpenSDK.Entities.Yggdrasil.GameProfile
                            {
                                GameId = serverId,
                        GameVersion = version.Name,
                        BootstrapMd5 = pair.BootstrapMd5,
                        DatFileMd5 = pair.DatFileMd5,
                        Mods = JsonSerializer.Deserialize<Codexus.OpenSDK.Entities.Yggdrasil.ModList>(mods)!,
                        User = new Codexus.OpenSDK.Entities.Yggdrasil.UserProfile { UserId = int.Parse(available.UserId), UserToken = currentToken }
                    }, certification);
                    if (success.IsSuccess)
                    {
                        if (AppState.Debug) Log.Information("消息认证成功");
                    }
                    else
                    {
                        if (AppState.Debug) Log.Error(new Exception(success.Error ?? "未知错误"), "消息认证失败，详细信息: {Error}", success.Error);
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
        _lastIp = interceptor.LocalAddress;
        _lastPort = interceptor.LocalPort;

        await X19.InterconnectionApi.GameStartAsync(available.UserId, available.AccessToken, serverId);
        return true;
    }
}
