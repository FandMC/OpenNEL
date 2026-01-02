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
using System.Collections.Concurrent;
using Codexus.Development.SDK.Connection;
using Serilog;

namespace OpenNEL.IRC;

public class IrcChatEventArgs : EventArgs
{
    public string Username { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public static class IrcManager
{
    static readonly ConcurrentDictionary<GameConnection, IrcClient> _clients = new();
    
    public static Func<string>? TokenProvider { get; set; }
    public static string Hwid { get; set; } = string.Empty;
    public static Action<GameConnection>? OnClientRemoved { get; set; }

    public static IrcClient GetOrCreate(GameConnection conn)
    {
        return _clients.GetOrAdd(conn, c => new IrcClient(c, TokenProvider, Hwid));
    }

    public static IrcClient? Get(GameConnection conn)
    {
        return _clients.TryGetValue(conn, out var client) ? client : null;
    }

    public static void Remove(GameConnection conn)
    {
        if (_clients.TryRemove(conn, out var client))
        {
            client.Stop();
            OnClientRemoved?.Invoke(conn);
            Log.Information("[IRC] 已移除: {Id}", conn.GameId);
        }
    }
}
