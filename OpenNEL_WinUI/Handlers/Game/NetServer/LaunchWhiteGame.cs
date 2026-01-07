/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Codexus.Cipher.Entities;
using Codexus.Cipher.Entities.WPFLauncher.NetGame.Texture;
using Codexus.Cipher.Protocol;
using Codexus.OpenSDK;
using Codexus.Game.Launcher.Entities;
using Codexus.Game.Launcher.Services.Java;
using Codexus.Game.Launcher.Utils;
using OpenNEL_WinUI.Manager;
using OpenNEL_WinUI.type;
using OpenNEL_WinUI.Entities.Web.NetGame;
using Serilog;

namespace OpenNEL_WinUI.Handlers.Game.NetServer;

public class LaunchWhiteGame
{
    private readonly IProgress<EntityProgressUpdate>? _progress;

    public LaunchWhiteGame(IProgress<EntityProgressUpdate>? progress = null)
    {
        _progress = progress;
    }

    public async Task<JoinGameResult> Execute(string accountId, string serverId, string serverName, string roleId)
    {
        if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(serverId) || string.IsNullOrWhiteSpace(roleId))
        {
            return new JoinGameResult { Success = false, Message = "参数错误" };
        }

        var available = UserManager.Instance.GetAvailableUser(accountId);
        if (available == null)
        {
            return new JoinGameResult { NotLogin = true };
        }

        try
        {
            PathUtil.EnsureDirectoriesExist();
            var result = await LaunchAsync(available.UserId, available.AccessToken, serverId, serverName, roleId);
            return result ? new JoinGameResult { Success = true } : new JoinGameResult { Success = false, Message = "启动失败" };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "白端启动失败");
            return new JoinGameResult { Success = false, Message = ex.Message };
        }
    }

    private async Task<bool> LaunchAsync(string userId, string accessToken, string serverId, string serverName, string roleId)
    {
        Log.Debug("白端启动: userId={UserId}, serverId={ServerId}, role={Role}", userId, serverId, roleId);

        var wpf = new WPFLauncher();
        var details = AppState.X19.QueryNetGameDetailById(userId, accessToken, serverId);
        var address = AppState.X19.GetNetGameServerAddress(userId, accessToken, serverId);
        
        var serverIp = address.Data!.Ip;
        var serverPort = address.Data!.Port;
        if (serverPort <= 0 && details.Data != null)
        {
            Log.Warning("服务器地址 API 返回端口为 0，尝试使用详情中的地址: {Addr}:{Port}", details.Data.ServerAddress, details.Data.ServerPort);
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

        Log.Debug("白端启动: 游戏版本={Version}, IP={Ip}, Port={Port}", version.Name, serverIp, serverPort);

        ReportProgress(10, "正在安装游戏模组");
        var serverMod = await InstallerService.InstallGameMods(userId, accessToken, gameVersion, wpf, serverId, false);
        var mods = JsonSerializer.Serialize(serverMod);

        ReportProgress(50, "正在启动游戏");
        var launchRequest = new EntityLaunchGame
        {
            GameName = serverName,
            GameId = serverId,
            RoleName = roleId,
            UserId = userId,
            ClientType = EnumGameClientType.Java,
            GameType = EnumGType.ServerGame,
            GameVersionId = (int)gameVersion,
            GameVersion = version.Name,
            AccessToken = accessToken,
            ServerIp = serverIp,
            ServerPort = serverPort,
            MaxGameMemory = 4096,
            LoadCoreMods = true
        };

        var launcher = LauncherService.CreateLauncher(launchRequest, accessToken, wpf, wpf.MPay.GameVersion, _progress ?? new Progress<EntityProgressUpdate>());
        GameManager.Instance.AddLauncher(launcher);

        await InterConn.GameStart(userId, accessToken, serverId);
        await X19.InterconnectionApi.GameStartAsync(userId, accessToken, serverId);

        ReportProgress(100, "启动完成");
        Log.Information("白端启动成功: ServerId={ServerId}, Role={Role}, ServerAddress={Addr}:{Port}", serverId, roleId, serverIp, serverPort);
        return true;
    }

    private void ReportProgress(int percent, string message)
    {
        _progress?.Report(new EntityProgressUpdate
        {
            Id = Guid.Empty,
            Percent = percent,
            Message = message
        });
    }
}
