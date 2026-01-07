/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Codexus.Cipher.Protocol;
using OpenNEL_WinUI.Manager;
using OpenNEL_WinUI.type;
using OpenNEL_WinUI.Entities.Web.RentalGame;
using Codexus.Development.SDK.Entities;
using Codexus.Game.Launcher.Services.Java;
using Codexus.Game.Launcher.Utils;
using Codexus.Interceptors;
using OpenNEL.Core.Utils;
using Codexus.OpenSDK;
using Serilog;

namespace OpenNEL_WinUI.Handlers.Game.RentalServer;

public class JoinRentalGame
{
    private EntityJoinRentalGame? _request;
    private string _lastIp = string.Empty;
    private int _lastPort;

    public async Task<JoinRentalGameResult> Execute(EntityJoinRentalGame request)
    {
        _request = request;
        var serverId = _request.ServerId;
        var serverName = _request.ServerName;
        var role = _request.Role;
        var password = _request.Password;
        var mcVersion = _request.McVersion;
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null) return new JoinRentalGameResult { NotLogin = true };
        if (string.IsNullOrWhiteSpace(serverId) || string.IsNullOrWhiteSpace(role))
        {
            return new JoinRentalGameResult { Success = false, Message = "参数错误" };
        }
        try
        {
            var ok = await StartAsync(serverId, serverName, role, password, mcVersion);
            if (!ok) return new JoinRentalGameResult { Success = false, Message = "启动失败" };
            return new JoinRentalGameResult { Success = true, Ip = _lastIp, Port = _lastPort };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加入租赁服失败");
            return new JoinRentalGameResult { Success = false, Message = ex.Message };
        }
    }

    public async Task<bool> StartAsync(string serverId, string serverName, string roleId, string password, string mcVersion)
    {
        Log.Debug("[RentalServer] StartAsync: serverId={ServerId}, role={Role}, mcVersion={Version}", serverId, roleId, mcVersion);
        var available = UserManager.Instance.GetLastAvailableUser();
        if (available == null) return false;

        var pwd = string.IsNullOrWhiteSpace(password) ? null : password;
        var addressResult = AppState.X19.GetRentalGameServerAddress(available.UserId, available.AccessToken, serverId, pwd);
        Log.Debug("[RentalServer] GetRentalGameServerAddress result: Code={Code}, Message={Message}, Data={Data}", 
            addressResult.Code, addressResult.Message, JsonSerializer.Serialize(addressResult.Data));
        if (addressResult.Data == null)
        {
            Log.Error("无法获取租赁服地址");
            return false;
        }

        var serverIp = addressResult.Data.McServerHost;
        var serverPort = addressResult.Data.McServerPort;
        Log.Information("租赁服地址: {Ip}:{Port}", serverIp, serverPort);

        var roles = AppState.X19.GetRentalGameRolesList(available.UserId, available.AccessToken, serverId);
        var selected = roles.Data.FirstOrDefault(r => r.Name == roleId);
        if (selected == null)
        {
            Log.Error("[RentalServer] 找不到角色: {RoleId}", roleId);
            return false;
        }

        var versionName = mcVersion;
        var versionMatch = System.Text.RegularExpressions.Regex.Match(versionName, @"(\d+\.\d+\.\d+)");
        string fullVersion = "";
        string shortVersion = "";
        if (versionMatch.Success)
        {
            fullVersion = versionMatch.Groups[1].Value; 
            var parts = fullVersion.Split('.');
            shortVersion = parts[0] + "." + parts[1];
        }
        else
        {
            var shortMatch = System.Text.RegularExpressions.Regex.Match(versionName, @"(\d+\.\d+)");
            if (shortMatch.Success)
            {
                shortVersion = shortMatch.Groups[1].Value;
            }
        }
        
        string resolvedVersion = "";
        if (!string.IsNullOrEmpty(fullVersion) && Md5Mapping.TryGetMd5FromGameVersion(fullVersion, out _))
        {
            resolvedVersion = fullVersion;
        }
        else if (!string.IsNullOrEmpty(shortVersion) && Md5Mapping.TryGetMd5FromGameVersion(shortVersion, out _))
        {
            resolvedVersion = shortVersion;
        }
        else
        {
            resolvedVersion = !string.IsNullOrEmpty(fullVersion) ? fullVersion : shortVersion;
        }
        
        versionName = resolvedVersion;
        Log.Debug("[RentalServer] 解析版本: {Original} -> full={Full}, short={Short}, resolved={Resolved}", mcVersion, fullVersion, shortVersion, versionName);
        var gameVersion = GameVersionUtil.GetEnumFromGameVersion(versionName);

        var serverMod = await InstallerService.InstallGameMods(
            available.UserId,
            available.AccessToken,
            gameVersion,
            AppState.X19,
            serverId,
            true);
        var mods = JsonSerializer.Serialize(serverMod);
        SemaphoreSlim authorizedSignal = new SemaphoreSlim(0);
        var pair = Md5Mapping.GetMd5FromGameVersion(versionName);

        _lastIp = serverIp;
        _lastPort = serverPort;

        var socksCfg = _request?.Socks5;
        var socksAddr = socksCfg != null ? (socksCfg.Address ?? string.Empty) : string.Empty;
        var socksPort = socksCfg != null ? socksCfg.Port : 0;
        Log.Information("JoinRentalGame SOCKS5 配置: Address={Addr}, Port={Port}, Username={User}", socksAddr, socksPort, socksCfg?.Username);
        if (!string.IsNullOrWhiteSpace(socksAddr) && socksPort <= 0) return false;
        if (!string.IsNullOrWhiteSpace(socksAddr) && socksPort > 0)
        {
            try { Dns.GetHostAddresses(socksAddr); }
            catch { return false; }
        }

        Interceptor interceptor = Interceptor.CreateInterceptor(
            _request?.Socks5 ?? new EntitySocks5(),
            mods,
            serverId,
            serverName,
            versionName,
            serverIp,
            serverPort,
            roleId,
            available.UserId,
            available.AccessToken,
            delegate(string certification)
            {
                Log.Logger.Information("Rental server certification: {Certification}", certification);
                Task.Run(async delegate
                {
                    try
                    {
                        var salt = CrcSalt.GetCached();
                        Log.Information("租赁服 CrcSalt: {Salt}", salt);
                        AppState.Services?.RefreshYggdrasil();
                        var latest = UserManager.Instance.GetAvailableUser(available.UserId);
                        var currentToken = latest?.AccessToken ?? available.AccessToken;
                        var success = await AppState.Services!.Yggdrasil.JoinServerAsync(new Codexus.OpenSDK.Entities.Yggdrasil.GameProfile
                        {
                            GameId = serverId,
                            GameVersion = versionName,
                            BootstrapMd5 = pair.BootstrapMd5,
                            DatFileMd5 = pair.DatFileMd5,
                            Mods = JsonSerializer.Deserialize<Codexus.OpenSDK.Entities.Yggdrasil.ModList>(mods)!,
                            User = new Codexus.OpenSDK.Entities.Yggdrasil.UserProfile { UserId = int.Parse(available.UserId), UserToken = currentToken }
                        }, certification);
                        if (success.IsSuccess)
                        {
                            if (AppState.Debug) Log.Information("租赁服消息认证成功");
                        }
                        else
                        {
                            Log.Error("租赁服消息认证失败: {Error}", success.Error);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "租赁服认证过程中发生异常");
                    }
                    finally
                    {
                        authorizedSignal.Release();
                    }
                });
                authorizedSignal.Wait();
            });

        InterConn.GameStart(available.UserId, available.AccessToken, _request?.GameId ?? serverId).GetAwaiter().GetResult();
        GameManager.Instance.AddInterceptor(interceptor);
        _lastIp = interceptor.LocalAddress;
        _lastPort = interceptor.LocalPort;

        await X19.InterconnectionApi.GameStartAsync(available.UserId, available.AccessToken, serverId);
        return true;
    }
}
