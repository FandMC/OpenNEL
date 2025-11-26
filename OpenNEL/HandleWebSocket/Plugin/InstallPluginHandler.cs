using OpenNEL.network;
using System.Text;
using System.Text.Json;
using Codexus.Development.SDK.Manager;
using OpenNEL.type;
using Serilog;
using System.Net.Http;
using System.IO;

namespace OpenNEL.HandleWebSocket.Plugin;

internal class InstallPluginHandler : IWsHandler
{
    public string Type => "install_plugin";
    public async Task ProcessAsync(System.Net.WebSockets.WebSocket ws, JsonElement root)
    {
        try
        {
            var infoStr = root.TryGetProperty("info", out var infoEl) ? infoEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(infoStr)) return;
            using var doc = JsonDocument.Parse(infoStr);
            var info = doc.RootElement;
            var pluginEl = info.TryGetProperty("plugin", out var pel) ? pel : default;
            if (pluginEl.ValueKind != JsonValueKind.Object) return;
            var id = pluginEl.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            var name = pluginEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
            var version = pluginEl.TryGetProperty("version", out var verEl) ? verEl.GetString() : null;
            var downloadUrl = pluginEl.TryGetProperty("downloadUrl", out var urlEl) ? urlEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(downloadUrl) || string.IsNullOrWhiteSpace(id)) return;
            Log.Information("安装插件 {PluginId} {PluginName} {PluginVersion}", id, name, version);
            var http = new HttpClient();
            var bytes = await http.GetByteArrayAsync(downloadUrl);
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var dir = Path.Combine(baseDir, "plugins");
            Directory.CreateDirectory(dir);
            string fileName;
            try
            {
                var uri = new Uri(downloadUrl);
                var candidate = Path.GetFileName(uri.AbsolutePath);
                fileName = string.IsNullOrWhiteSpace(candidate) ? (id + ".ug") : candidate;
            }
            catch { fileName = id + ".ug"; }
            var path = Path.Combine(dir, fileName);
            File.WriteAllBytes(path, bytes);
            try { PluginManager.Instance.LoadPlugins("plugins"); } catch { }
            var upd = JsonSerializer.Serialize(new { type = "installed_plugins_updated" });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(upd)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
            var items = PluginManager.Instance.Plugins.Values.Select(plugin => new {
                identifier = plugin.Id,
                name = plugin.Name,
                version = plugin.Version,
                description = plugin.Description,
                author = plugin.Author,
                status = plugin.Status,
                waitingRestart = AppState.WaitRestartPlugins.ContainsKey(plugin.Id)
            }).ToArray();
            var msg = JsonSerializer.Serialize(new { type = "installed_plugins", items });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "安装插件失败");
        }
    }
}