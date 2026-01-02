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
using OpenNEL.IRC.Packet;
using Codexus.Development.SDK.Connection;
using Serilog;

namespace OpenNEL.IRC;

public class IrcClient : IDisposable
{
    readonly GameConnection _conn;
    readonly IrcConnection _irc;
    readonly CancellationTokenSource _cts = new();
    
    Timer? _heartbeat;
    volatile bool _running;

    public IReadOnlyDictionary<string, string> Players => _irc.Players;
    public event EventHandler<IrcChatEventArgs>? ChatReceived;
    public GameConnection Connection => _conn;
    public string ServerId => _conn.GameId;

    public IrcClient(GameConnection conn, Func<string>? tokenProvider, string hwid)
    {
        _conn = conn;
        _irc = new IrcConnection("api.fandmc.cn", 9527);
        _irc.SetCredentials(tokenProvider?.Invoke() ?? "", hwid, conn.GameId);
        _irc.ChatReceived += OnChat;
    }

    public void Start(string playerName)
    {
        if (_running) return;
        _running = true;

        Log.Information("[IRC] 启动: {Id}, 玩家: {Name}", ServerId, playerName);
        Task.Run(() => Initialize(playerName), _cts.Token);
    }

    public void Stop()
    {
        if (!_running) return;
        _running = false;

        _heartbeat?.Dispose();
        _irc.Disconnect();
        Log.Information("[IRC] 停止: {Id}", ServerId);
    }

    public void SendChat(string player, string msg) => _irc.SendChat(player, msg);

    public void Dispose()
    {
        _cts.Cancel();
        Stop();
        _cts.Dispose();
    }

    void Initialize(string playerName)
    {
        SendStatus("§e[§bIRC§e] 正在连接 IRC 服务器...");

        if (!_irc.Connect())
        {
            SendStatus("§c[§bIRC§c] IRC 连接失败，将自动重试");
        }
        else
        {
            _irc.Report(playerName);
            SendStatus("§a[§bIRC§a] IRC 连接成功 Ciallo～(∠・ω< )⌒");
        }

        Task.Run(ListenLoop, _cts.Token);
        _heartbeat = new Timer(_ => OnHeartbeat(), null, 20000, 20000);
    }

    void ListenLoop()
    {
        while (_running && !_cts.Token.IsCancellationRequested)
        {
            var line = _irc.ReadLine();
            
            if (line == null)
            {
                if (_running) _irc.Reconnect();
                continue;
            }

            if (!string.IsNullOrEmpty(line))
                _irc.ProcessLine(line);
        }
    }

    void OnHeartbeat()
    {
        if (!_running) return;

        try
        {
            _irc.RefreshPlayers();
            var count = _irc.Players.Count;
            if (count > 0)
                SendStatus($"§e[§bIRC§e] 当前在线 {count} 人，使用 §a/irc 想说的话§e 聊天");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "[IRC] 心跳失败");
            if (_running) _irc.Reconnect();
        }
    }

    void OnChat(string username, string player, string message)
    {
        ChatReceived?.Invoke(this, new IrcChatEventArgs
        {
            Username = username,
            PlayerName = player,
            Message = message
        });
    }

    void SendStatus(string msg) => CChatCommandIrc.SendLocalMessage(_conn, msg);
}
