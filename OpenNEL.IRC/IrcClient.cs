using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenNEL.SDK.Connection;
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

    public static IReadOnlyCollection<IrcClient> GetAllClients() => _clients.Values.ToList().AsReadOnly();

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
            Log.Information("[IRC] 已移除连接: {Id}", conn.GameId);
        }
    }

    public static string? GetUsername(string name)
    {
        foreach (var client in _clients.Values)
        {
            var u = client.GetUsername(name);
            if (u != null) return u;
        }
        return null;
    }

    public static IReadOnlyDictionary<string, string> GetAllOnlinePlayers()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var client in _clients.Values)
            foreach (var (k, v) in client.Players)
                result.TryAdd(k, v);
        return result;
    }
}

public class IrcClient : IDisposable
{
    const string HOST = "api.fandmc.cn";
    const int PORT = 9527;
    const int HEARTBEAT_MS = 25000;

    readonly GameConnection _conn;
    readonly Func<string>? _tokenProvider;
    readonly string _hwid;
    
    TcpClient? _client;
    StreamReader? _reader;
    StreamWriter? _writer;
    Timer? _refreshTimer;
    Timer? _heartbeatTimer;
    Thread? _listenerThread;
    
    readonly object _connLock = new();
    readonly object _writeLock = new();
    readonly ManualResetEventSlim _responseEvent = new(false);
    readonly ConcurrentDictionary<string, string> _players = new(StringComparer.OrdinalIgnoreCase);
    
    bool _connected;
    bool _listening;
    bool _started;
    string? _response;

    public IReadOnlyDictionary<string, string> Players => _players;
    public event EventHandler<IrcChatEventArgs>? ChatReceived;
    public GameConnection Connection => _conn;
    public string ServerId => _conn.GameId;
    string Token => _tokenProvider?.Invoke() ?? string.Empty;

    public IrcClient(GameConnection conn, Func<string>? tokenProvider, string hwid)
    {
        _conn = conn;
        _tokenProvider = tokenProvider;
        _hwid = hwid;
    }

    public void Start()
    {
        if (_started) return;
        _started = true;
        
        Task.Run(() => { Connect(); StartListener(); });
        _refreshTimer = new Timer(_ => Refresh(), null, 1000, 5000);
        _heartbeatTimer = new Timer(_ => Heartbeat(), null, HEARTBEAT_MS, HEARTBEAT_MS);
        Log.Information("[IRC] 启动: {Id}", ServerId);
    }

    public void Stop()
    {
        if (!_started) return;
        _started = false;
        _listening = false;
        _refreshTimer?.Dispose();
        _heartbeatTimer?.Dispose();
        _refreshTimer = _heartbeatTimer = null;
        Disconnect();
        _players.Clear();
        Log.Information("[IRC] 停止: {Id}", ServerId);
    }

    void Connect()
    {
        lock (_connLock)
        {
            if (_connected) return;
            try
            {
                _client = new TcpClient();
                _client.Connect(HOST, PORT);
                var stream = _client.GetStream();
                _reader = new StreamReader(stream, Encoding.UTF8);
                _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                _connected = true;
                Log.Information("[IRC:{Id}] 已连接", ServerId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[IRC:{Id}] 连接失败", ServerId);
                _connected = false;
            }
        }
    }

    void Disconnect()
    {
        lock (_connLock)
        {
            _connected = false;
            try { _writer?.Dispose(); } catch { }
            try { _reader?.Dispose(); } catch { }
            try { _client?.Dispose(); } catch { }
            _writer = null;
            _reader = null;
            _client = null;
        }
    }

    void Reconnect()
    {
        if (!_started) return;
        Log.Warning("[IRC:{Id}] 重连...", ServerId);
        Disconnect();
        Thread.Sleep(2000);
        Connect();
    }

    void Heartbeat()
    {
        if (!_started) return;
        if (!_connected) { Reconnect(); return; }
        try { RefreshPlayers(); }
        catch { Reconnect(); }
    }

    void Refresh()
    {
        if (!_started) return;
        try { RefreshPlayers(); } catch { }
    }

    void StartListener()
    {
        if (_listening) return;
        _listening = true;
        _listenerThread = new Thread(Listen) { IsBackground = true, Name = $"IRC-{ServerId}" };
        _listenerThread.Start();
    }

    void Listen()
    {
        while (_listening && _started)
        {
            try
            {
                if (!_connected || _reader == null)
                {
                    Thread.Sleep(2000);
                    if (_started) Reconnect();
                    continue;
                }
                
                string? line;
                try { line = _reader.ReadLine(); }
                catch { _connected = false; continue; }
                
                if (string.IsNullOrEmpty(line)) continue;
                ProcessLine(line);
            }
            catch (Exception ex)
            {
                if (_listening) { Log.Warning(ex, "[IRC:{Id}] 监听异常", ServerId); Thread.Sleep(1000); }
            }
        }
    }

    void ProcessLine(string line)
    {
        var p = line.Split('|');
        if (p.Length < 2) return;

        switch (p[0])
        {
            case "CHAT" when p.Length >= 4:
                ChatReceived?.Invoke(this, new IrcChatEventArgs
                {
                    Username = p[1],
                    PlayerName = p[2],
                    Message = string.Join("|", p.Skip(3))
                });
                break;
            case "OK":
            case "ERR":
                _response = line;
                _responseEvent.Set();
                break;
        }
    }

    string? Send(string cmd, bool wait = true)
    {
        lock (_writeLock)
        {
            if (!_connected || _writer == null) { Reconnect(); if (!_connected) return null; }
            
            try
            {
                if (wait) { _responseEvent.Reset(); _response = null; }
                _writer.WriteLine(cmd);
                if (!wait) return "OK";
                return _responseEvent.Wait(5000) ? _response : null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[IRC:{Id}] 发送失败", ServerId);
                Reconnect();
                return null;
            }
        }
    }

    public bool ReportPlayer(string name)
    {
        var r = Send($"REPORT|{Token}|{_hwid}|{ServerId}|{name}");
        if (r?.StartsWith("OK|") == true) { Log.Information("[IRC:{Id}] 上报: {Name}", ServerId, name); return true; }
        Log.Warning("[IRC:{Id}] 上报失败: {Name}", ServerId, name);
        return false;
    }

    public void SendChat(string name, string msg)
    {
        Send($"CHAT|{Token}|{_hwid}|{ServerId}|{name}|{msg}", false);
        Log.Debug("[IRC:{Id}] 发送: {Name}: {Msg}", ServerId, name, msg);
    }

    public bool RefreshPlayers()
    {
        var r = Send($"GET|{Token}|{_hwid}|{ServerId}|");
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

    public string? GetUsername(string name) => _players.TryGetValue(name, out var u) ? u : null;
    public void Dispose() => Stop();

    class PlayerInfo
    {
        [JsonPropertyName("Username")] public string Username { get; set; } = string.Empty;
        [JsonPropertyName("PlayerName")] public string PlayerName { get; set; } = string.Empty;
    }
}
