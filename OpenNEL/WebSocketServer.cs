using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using OpenNEL.HandleWebSocket;
using OpenNEL.type;
using OpenNEL.Utils;

namespace OpenNEL;

internal class WebSocketServer
{
    private readonly ConcurrentDictionary<Guid, WebSocket> _clients = new();
    private CancellationTokenSource? _cts;
    private HttpListener? _listener;
    private volatile bool _running;
    private int _currentPort;

    public async Task StartAsync(bool listenAll = false)
    {
        if (_running) return;
        _currentPort = GetPort();
        _cts = new CancellationTokenSource();
        var started = false;
        while (!started && _currentPort <= 65535)
        {
            _listener = new HttpListener();
            try
            {
                var host = listenAll ? "*" : "127.0.0.1";
                _listener.Prefixes.Add($"http://{host}:{_currentPort}/");
                if (!listenAll) _listener.Prefixes.Add($"http://localhost:{_currentPort}/");
                _listener.Start();
                started = true;
                Log.Information("-> 访问: http://127.0.0.1:{Port}/ 使用OpenNEL", _currentPort);
                var url = $"http://127.0.0.1:{_currentPort}/";
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "浏览器打开失败");
                }
                if (listenAll)
                {
                    var addrs = NetworkInterface
                        .GetAllNetworkInterfaces()
                        .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                        .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                        .Select(a => a.Address.ToString());
                    foreach (var ip in addrs)
                    {
                        Log.Information("LAN地址: {Address}", $"http://{ip}:{_currentPort}/");
                    }
                }
            }
            catch (HttpListenerException ex)
            {
                Log.Warning("端口 {Port} 已被占用，尝试下一个端口。错误: {Error}", _currentPort, ex.Message);
                try { _listener.Close(); } catch { }
                _currentPort++;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "监听启动失败 {Port}", _currentPort);
                try { _listener?.Close(); } catch { }
                return;
            }
        }
        if (!started)
        {
            Log.Error("端口无效，无法启动");
            return;
        }
        _running = true;
        _ = ProcessRequestsAsync(_cts.Token);
    }

    int GetPort()
    {
        var env = Environment.GetEnvironmentVariable("NEL_PORT");
        if (int.TryParse(env, out var p) && p > 0) return p;
        return 8080;
    }

    async Task ServeContextAsync(HttpListenerContext context)
    {
        var req = context.Request;
        if (AppState.Debug)
        {
            Log.Information("HTTP {Method} {Url}", req.HttpMethod, req.Url);
        }
        if (req.Url!.AbsolutePath == "/ws")
        {
            if (req.IsWebSocketRequest)
            {
                var wsCtx = await context.AcceptWebSocketAsync(null);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await HandleWebSocket(wsCtx.WebSocket);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "WS连接处理异常");
                    }
                });
                return;
            }
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        var root = Path.Combine(AppContext.BaseDirectory, "resources");
        var path = req.Url.AbsolutePath;
        if (path == "/") path = "/index.html";
        if (path == "/favicon.ico") path = "/favicon.ico";
        if (path == "/assets/favicon-DkNf_H3x.ico") path = "/assets/favicon-DkNf_H3x.ico";
        
        var filePath = Path.GetFullPath(Path.Combine(root, path.TrimStart('/')));
        if (!filePath.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(filePath))
        {
            context.Response.StatusCode = 404;
            context.Response.Close();
            return;
        }
        await WriteFileResponse(context.Response, filePath);
    }

    async Task HandleWebSocket(WebSocket ws)
    {
        var id = Guid.NewGuid();
        _clients.TryAdd(id, ws);
        var buffer = new byte[4096];
        var connectedMsg = "connected";
        if (AppState.Debug)
        {
            Log.Information("WS Send: {Text}", connectedMsg);
        }
        await SendText(ws, connectedMsg);
        while (ws.State == WebSocketState.Open && _running)
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                break;
            }
            var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
            if (AppState.Debug)
            {
                Log.Information("WS Recv: {Text}", text);
            }
            try
            {
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;
                var type = root.TryGetProperty("type", out var t) ? t.GetString() : null;
                if (!string.IsNullOrWhiteSpace(type))
                {
                    var handler = HandlerFactory.Get(type);
                    if (handler != null)
                    {
                        object? payload = null;
                        try
                        {
                            payload = await handler.ProcessAsync(root);
                        }
                        catch (Exception ex)
                        {
                            var et = type + "_error";
                            payload = new { type = et, message = ex.Message };
                        }
                        if (payload != null)
                        {
                            await Send(ws, payload);
                        }
                        continue;
                    }
                }
                if (AppState.Debug)
                {
                    Log.Information("WS Echo: {Text}", text);
                }
                await SendText(ws, text);
            }
            catch
            {
                if (AppState.Debug)
                {
                    Log.Information("WS Echo on error: {Text}", text);
                }
                await SendText(ws, text);
            }
        }
        try
        {
            if (ws.State == WebSocketState.Open)
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        }
        catch { }
        _clients.TryRemove(id, out _);
    }

    async Task Send(WebSocket ws, object payload)
    {
        var seq = payload as System.Collections.IEnumerable;
        if (seq != null && !(payload is string))
        {
            foreach (var item in seq)
            {
                if (item == null) continue;
                var msg = JsonSerializer.Serialize(item);
                await SendText(ws, msg);
            }
            return;
        }
        var text = JsonSerializer.Serialize(payload);
        await SendText(ws, text);
    }

    async Task SendText(WebSocket ws, string text)
    {
        await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(text)), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    async Task WriteFileResponse(HttpListenerResponse resp, string filePath)
    {
        var content = await File.ReadAllBytesAsync(filePath);
        resp.ContentType = MimeTypes.Get(filePath);
        resp.ContentLength64 = content.Length;
        await resp.OutputStream.WriteAsync(content, 0, content.Length);
        resp.Close();
    }

    async Task ProcessRequestsAsync(CancellationToken token)
    {
        try
        {
            while (_running && !token.IsCancellationRequested)
            {
                if (_listener == null)
                {
                    Log.Warning("WS服务未运行");
                    break;
                }
                var getTask = _listener.GetContextAsync();
                var completed = await Task.WhenAny(getTask, Task.Delay(-1, token));
                if (token.IsCancellationRequested) break;
                if (completed != getTask) continue;
                var ctx = await getTask;
                _ = Task.Run(async () =>
                {
                    try { await ServeContextAsync(ctx); }
                    catch (Exception ex) { Log.Error(ex, "请求处理异常"); }
                }, token);
            }
        }
        catch (ObjectDisposedException)
        {
            Log.Debug("HttpListener已释放");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "请求处理失败");
        }
    }

    public async Task StopAsync()
    {
        if (!_running) return;
        _running = false;
        if (_cts != null) await _cts.CancelAsync();
        var closing = _clients
            .Where(kv => kv.Value.State == WebSocketState.Open)
            .Select(kv => kv.Value.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server closed", CancellationToken.None))
            .ToList();
        if (closing.Count > 0) await Task.WhenAll(closing);
        _clients.Clear();
        try { _listener?.Close(); } catch { }
        Log.Information("WS服务已停止");
    }
}
