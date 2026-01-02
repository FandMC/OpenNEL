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
using DotNetty.Buffers;
using OpenNEL.IRC.Packet;
using Codexus.Development.SDK.Connection;
using Codexus.Development.SDK.Enums;
using Codexus.Development.SDK.Event;
using Codexus.Development.SDK.Extensions;
using Codexus.Development.SDK.Manager;
using Codexus.Development.SDK.Utils;
using Serilog;
using System.Collections.Concurrent;

namespace OpenNEL.IRC;

public static class IrcEventHandler
{
    static readonly ConcurrentDictionary<GameConnection, bool> _processed = new();

    public static void Register(Func<string> tokenProvider, string hwid)
    {
        IrcManager.TokenProvider = tokenProvider;
        IrcManager.Hwid = hwid;
        IrcManager.OnClientRemoved = conn => _processed.TryRemove(conn, out _);

        foreach (var channel in MessageChannels.AllVersions)
        {
            EventManager.Instance.RegisterHandler<EventLoginSuccess>(channel, OnLoginSuccess);
        }

        EventManager.Instance.RegisterHandler<EventConnectionClosed>("channel_connection", OnConnectionClosed);
    }

    static void OnLoginSuccess(EventLoginSuccess args)
    {
        var nickName = args.Connection.NickName;
        if (string.IsNullOrEmpty(nickName)) return;

        if (!_processed.TryAdd(args.Connection, true)) return;

        var client = IrcManager.GetOrCreate(args.Connection);
        client.ChatReceived += OnChatReceived;
        client.Start(nickName);
    }

    static void OnConnectionClosed(EventConnectionClosed args)
    {
        IrcManager.Remove(args.Connection);
    }

    static void OnChatReceived(object? sender, IrcChatEventArgs e)
    {
        if (sender is not IrcClient client) return;
        CChatCommandIrc.SendLocalMessage(client.Connection, $"{e.Username} <{e.PlayerName}> {e.Message}");
    }
}
