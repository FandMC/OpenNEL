using OpenNEL.Network;
using System.Text.Json;
using Codexus.Development.SDK.Manager;
using OpenNEL.type;
using Serilog;
using System.Net.Http;
using System.IO;

namespace OpenNEL.Message.Plugin;

internal class UpdatePluginMessage : IWsMessage
{
    public string Type => "update_plugin";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        try
        {
            var pluginId = root.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            var oldVersion = root.TryGetProperty("old", out var oldEl) ? oldEl.GetString() : null;
            var infoStr = root.TryGetProperty("info", out var infoEl) ? infoEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(pluginId) || string.IsNullOrWhiteSpace(infoStr)) return null;
            using var doc = JsonDocument.Parse(infoStr);
            var pluginEl = doc.RootElement.TryGetProperty("plugin", out var pel) ? pel : default;
            if (pluginEl.ValueKind != JsonValueKind.Object) return null;
            var name = pluginEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
            var newVersion = pluginEl.TryGetProperty("version", out var verEl) ? verEl.GetString() : null;
            var downloadUrl = pluginEl.TryGetProperty("downloadUrl", out var urlEl) ? urlEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(downloadUrl)) return null;
            Log.Information("更新插件 {PluginId} {PluginName} {OldVersion} -> {NewVersion}", pluginId, name, oldVersion, newVersion);
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
                fileName = string.IsNullOrWhiteSpace(candidate) ? (pluginId + ".ug") : candidate;
            }
            catch { fileName = pluginId + ".ug"; }
            var path = Path.Combine(dir, fileName);
            File.WriteAllBytes(path, bytes);
            try
            {
                if (PluginManager.Instance.HasPlugin(pluginId))
                {
                    PluginManager.Instance.UninstallPlugin(pluginId);
                }
                AppState.WaitRestartPlugins[pluginId] = true;
            }
            catch { }
            var updPayload = new { type = "installed_plugins_updated" };
            var items = PluginManager.Instance.Plugins.Values.Select(plugin => new {
                identifier = plugin.Id,
                name = plugin.Name,
                version = plugin.Version,
                description = plugin.Description,
                author = plugin.Author,
                status = plugin.Status,
                waitingRestart = AppState.WaitRestartPlugins.ContainsKey(plugin.Id)
            }).ToArray();
            var listPayload = new { type = "installed_plugins", items };
            return new object[] { updPayload, listPayload };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "更新插件失败");
            return new { type = "update_plugin_error", message = ex.Message };
        }
    }
}
