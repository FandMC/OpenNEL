/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Codexus.Interceptors;
using Codexus.Development.SDK.Entities;
using OpenNEL_WinUI.Entities.Web.NetGame;
using OpenNEL_WinUI.Handlers.Game.NetServer;
using OpenNEL_WinUI.type;
using Serilog;

namespace OpenNEL_WinUI.Manager;

public static class BannedRoleTracker
{
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, HashSet<string>>> _bannedRoles = new();

    public static void MarkBanned(string userId, string serverId, string roleName)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(serverId) || string.IsNullOrWhiteSpace(roleName))
            return;

        var userDict = _bannedRoles.GetOrAdd(userId, _ => new ConcurrentDictionary<string, HashSet<string>>());
        var serverSet = userDict.GetOrAdd(serverId, _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        
        lock (serverSet)
        {
            serverSet.Add(roleName);
        }
        
        Log.Information("[BannedRoleTracker] 标记封禁: UserId={UserId}, ServerId={ServerId}, Role={Role}", userId, serverId, roleName);
    }

    public static bool IsBanned(string userId, string serverId, string roleName)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(serverId) || string.IsNullOrWhiteSpace(roleName))
            return false;

        if (!_bannedRoles.TryGetValue(userId, out var userDict))
            return false;

        if (!userDict.TryGetValue(serverId, out var serverSet))
            return false;

        lock (serverSet)
        {
            return serverSet.Contains(roleName);
        }
    }

    public static List<string> GetAvailableRoles(string userId, string accessToken, string serverId)
    {
        try
        {
            var roles = AppState.X19.QueryNetGameCharacters(userId, accessToken, serverId);
            var allRoles = roles.Data?.Select(r => r.Name).ToList() ?? new List<string>();
            
            if (!_bannedRoles.TryGetValue(userId, out var userDict))
                return allRoles;

            if (!userDict.TryGetValue(serverId, out var serverSet))
                return allRoles;

            lock (serverSet)
            {
                return allRoles.Where(r => !serverSet.Contains(r)).ToList();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[BannedRoleTracker] 获取可用角色失败");
            return new List<string>();
        }
    }

    public static void ClearUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        _bannedRoles.TryRemove(userId, out _);
        Log.Information("[BannedRoleTracker] 清除用户记录: UserId={UserId}", userId);
    }

    public static void ClearAll()
    {
        _bannedRoles.Clear();
        Log.Information("[BannedRoleTracker] 清除所有记录");
    }

    public static async Task<bool> TrySwitchToAnotherRole(
        string userId, 
        string accessToken, 
        string serverId, 
        string serverName,
        string currentRole,
        EntitySocks5? socks5)
    {
        try
        {
            MarkBanned(userId, serverId, currentRole);

            var availableRoles = GetAvailableRoles(userId, accessToken, serverId);
            
            if (availableRoles.Count == 0)
            {
                Log.Warning("[BannedRoleTracker] 没有可用角色了: UserId={UserId}, ServerId={ServerId}", userId, serverId);
                NotificationHost.ShowGlobal("此账号在该服务器没有其他可用角色", ToastLevel.Warning);
                return false;
            }

            var nextRole = availableRoles.Last();
            Log.Information("[BannedRoleTracker] 切换到角色: {Role}", nextRole);

            var joinGame = new JoinGame();
            var request = new EntityJoinGame
            {
                ServerId = serverId,
                ServerName = serverName,
                Role = nextRole,
                Socks5 = socks5 ?? new EntitySocks5()
            };

            var result = await joinGame.Execute(request);
            if (result.Success)
            {
                NotificationHost.ShowGlobal($"已切换到角色: {nextRole}", ToastLevel.Success);
                return true;
            }
            else
            {
                Log.Warning("[BannedRoleTracker] 切换角色失败: {Message}", result.Message);
                NotificationHost.ShowGlobal($"切换角色失败: {result.Message}", ToastLevel.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[BannedRoleTracker] 切换角色异常");
            NotificationHost.ShowGlobal("切换角色时发生错误", ToastLevel.Error);
            return false;
        }
    }
}
