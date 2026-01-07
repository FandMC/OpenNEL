/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using OpenNEL.IRC.Packet;
using Codexus.Development.SDK.Connection;
using Serilog;

namespace OpenNEL.IRC;

public class IrcClient : IDisposable
{
    readonly GameConnection _conn;
    readonly string _token;
    readonly string _hwid;
    readonly IrcPlayerList _players = new();

    TcpLineClient? _tcp;
    bool _welcomed;
    Timer? _timer;
    volatile bool _running;

    public string ServerId => _conn.GameId;
    public GameConnection Connection => _conn;
    public IReadOnlyDictionary<string, string> Players => _players.All;
    public event EventHandler<IrcChatEventArgs>? ChatReceived;

    public IrcClient(GameConnection conn, Func<string>? tokenProvider, string hwid)
    {
        _conn = conn;
        _token = tokenProvider?.Invoke() ?? "";
        _hwid = hwid;
    }

    public void Start(string playerName)
    {
        if (_running) return;
        _running = true;
        Log.Information("[IRC] 启动: {Id}, 玩家: {Name}", ServerId, playerName);
        Task.Run(() => Run(playerName));
    }

    public void Stop()
    {
        _running = false;
        _timer?.Dispose();
        _tcp?.Close();
    }

    public void SendChat(string player, string msg)
    {
        var cmd = IrcProtocol.Chat(_token, _hwid, ServerId, player, msg);
        Log.Information("[IRC] 发送: {Cmd}", cmd);
        _tcp?.Send(cmd);
    }

    public void Dispose()
    {
        Stop();
        _tcp?.Dispose();
    }

    void Run(string playerName)
    {
        while (_running)
        {
            try
            {
                _tcp = new TcpLineClient(IrcProtocol.Host, IrcProtocol.Port);
                _tcp.Connect();

                _welcomed = false;
                _tcp.Send(IrcProtocol.Report(_token, _hwid, ServerId, playerName));
                _timer = new Timer(_ => _tcp?.Send(IrcProtocol.Get(_token, _hwid, ServerId)), null, 1000, 20000);

                while (_running)
                {
                    var line = _tcp.Read();
                    if (line == null) break;
                    Process(line);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[IRC] 异常");
            }

            _timer?.Dispose();
            _tcp?.Close();
            if (_running) Thread.Sleep(3000);
        }
    }

    void Process(string line)
    {
        Log.Debug("[IRC] 收到: {Line}", line);
        var msg = IrcProtocol.Parse(line);
        if (msg == null) return;

        if (msg.IsPlayerList)
        {
            _players.Update(msg.Data);
            if (!_welcomed)
            {
                _welcomed = true;
                Msg("§a[§bIRC§a] IRC 连接成功 Ciallo～(∠・ω< )⌒");
            }
            if (_players.Count > 0)
                Msg($"§e[§bIRC§e] 当前在线 {_players.Count} 人，使用 §a/irc 想说的话§e 聊天");
        }
        else if (msg.IsChat)
        {
            ChatReceived?.Invoke(this, new IrcChatEventArgs
            {
                Username = msg.Parts[1],
                PlayerName = msg.Parts[2],
                Message = string.Join("|", msg.Parts.Skip(3))
            });
        }
    }

    void Msg(string msg)
    {
        Log.Information("[IRC] 显示消息: {Msg}, 版本: {Ver}", msg, _conn.ProtocolVersion);
        CChatCommandIrc.SendLocalMessage(_conn, msg);
    }
}
