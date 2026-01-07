/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenNEL.IRC;

public class IrcConnection : IDisposable
{
    readonly string _host;
    readonly int _port;
    readonly object _lock = new();
    readonly ManualResetEventSlim _responseEvent = new(false);
    readonly ConcurrentDictionary<string, string> _players = new(StringComparer.OrdinalIgnoreCase);
    
    TcpClient? _tcp;
    StreamReader? _reader;
    StreamWriter? _writer;
    string? _response;
    string? _playerName;
    string _token = string.Empty;
    string _hwid = string.Empty;
    string _serverId = string.Empty;

    public bool IsConnected { get; private set; }
    public IReadOnlyDictionary<string, string> Players => _players;
    public event Action<string, string, string>? ChatReceived;

    public IrcConnection(string host, int port) { _host = host; _port = port; }

    public void SetCredentials(string token, string hwid, string serverId)
    {
        _token = token;
        _hwid = hwid;
        _serverId = serverId;
    }

    public bool Connect()
    {
        lock (_lock)
        {
            if (IsConnected) return true;
            try
            {
                _tcp = new TcpClient();
                _tcp.Connect(_host, _port);
                _tcp.SendTimeout = 10000;
                _tcp.ReceiveTimeout = 10000;
                var stream = _tcp.GetStream();
                _reader = new StreamReader(stream, Encoding.UTF8);
                _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
                IsConnected = true;
                return true;
            }
            catch { IsConnected = false; return false; }
        }
    }

    public void Disconnect()
    {
        lock (_lock)
        {
            IsConnected = false;
            try { _writer?.Dispose(); } catch { }
            try { _reader?.Dispose(); } catch { }
            try { _tcp?.Dispose(); } catch { }
            _writer = null; _reader = null; _tcp = null;
        }
    }

    public void Reconnect()
    {
        Disconnect();
        Thread.Sleep(3000);
        if (Connect() && _playerName != null)
        {
            var name = _playerName;
            Task.Run(() =>
            {
                Thread.Sleep(500);
                Report(name);
            });
        }
    }

    public string? ReadLine()
    {
        if (!IsConnected || _reader == null) return null;
        try { return _reader.ReadLine(); }
        catch { IsConnected = false; return null; }
    }

    public void ProcessLine(string line)
    {
        if (line.StartsWith("OK") || line.StartsWith("ERR"))
        {
            _response = line;
            _responseEvent.Set();
            return;
        }

        var p = line.Split('|');
        if (p[0] == "CHAT" && p.Length >= 4)
            ChatReceived?.Invoke(p[1], p[2], string.Join("|", p.Skip(3)));
    }

    string? Send(string cmd, bool wait = true)
    {
        lock (_lock)
        {
            if (!IsConnected || _writer == null) return null;
            try
            {
                if (wait) { _responseEvent.Reset(); _response = null; }
                _writer.WriteLine(cmd);
                if (!wait) return "OK";
                return _responseEvent.Wait(5000) ? _response : null;
            }
            catch { IsConnected = false; return null; }
        }
    }

    public bool Report(string name)
    {
        var r = Send($"REPORT|{_token}|{_hwid}|{_serverId}|{name}");
        if (r?.StartsWith("OK|") != true) return false;
        
        _playerName = name;
        RefreshPlayers();
        return true;
    }

    public void SendChat(string player, string msg) => Send($"CHAT|{_token}|{_hwid}|{_serverId}|{player}|{msg}", false);

    public bool RefreshPlayers()
    {
        var r = Send($"GET|{_token}|{_hwid}|{_serverId}|");
        if (r?.StartsWith("OK|") != true) return false;
        try
        {
            var list = JsonSerializer.Deserialize<PlayerInfo[]>(r[3..]);
            _players.Clear();
            if (list != null) foreach (var p in list) _players[p.PlayerName] = p.Username;
            return true;
        }
        catch { return false; }
    }

    public void Dispose()
    {
        Disconnect();
        _responseEvent.Dispose();
    }

    class PlayerInfo
    {
        [JsonPropertyName("Username")] public string Username { get; set; } = string.Empty;
        [JsonPropertyName("PlayerName")] public string PlayerName { get; set; } = string.Empty;
    }
}
