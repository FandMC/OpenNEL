using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using Serilog;
using OpenNEL.HandleWebSocket;
using OpenNEL.type;

namespace OpenNEL;

internal class WebSocketServer
{
    public async Task StartAsync()
    {
        var port = GetPort();
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Prefixes.Add($"http://localhost:{port}/");
        Log.Information("-> 访问: http://127.0.0.1:{Port}/ 使用OpenNEL", port);
        listener.Start();
        var url = $"http://127.0.0.1:{port}/";
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "浏览器打开失败");
        }
        
        _ = Task.Run(async () =>
        {
            while (true)
            {
                var ctx = await listener.GetContextAsync();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ServeContextAsync(ctx);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "请求处理异常");
                    }
                });
            }
        });
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
        var buffer = new byte[4096];
        var connectedMsg = "connected";
        if (AppState.Debug)
        {
            Log.Information("WS Send: {Text}", connectedMsg);
        }
        await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(connectedMsg)), WebSocketMessageType.Text, true, CancellationToken.None);
        
        while (ws.State == WebSocketState.Open)
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
                        await handler.ProcessAsync(ws, root);
                        continue;
                    }
                }
                if (AppState.Debug)
                {
                    Log.Information("WS Echo: {Text}", text);
                }
                await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(text)), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch
            {
                if (AppState.Debug)
                {
                    Log.Information("WS Echo on error: {Text}", text);
                }
                await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(text)), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }

    async Task WriteFileResponse(HttpListenerResponse resp, string filePath)
    {
        var content = await File.ReadAllBytesAsync(filePath);
        resp.ContentType = GetMimeType(filePath);
        resp.ContentLength64 = content.Length;
        await resp.OutputStream.WriteAsync(content, 0, content.Length);
        resp.Close();
    }

    string GetMimeType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext == ".html") return "text/html";
        if (ext == ".css") return "text/css";
        if (ext == ".js") return "application/javascript";
        if (ext == ".png") return "image/png";
        if (ext == ".jpg" || ext == ".jpeg") return "image/jpeg";
        if (ext == ".svg") return "image/svg+xml";
        if (ext == ".ico") return "image/x-icon";
        return "application/octet-stream";
    }
}