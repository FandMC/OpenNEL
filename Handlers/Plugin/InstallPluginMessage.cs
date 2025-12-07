using System;
using System.Text.Json;
using Codexus.Development.SDK.Manager;
using OpenNEL.type;
using Serilog;
using System.Net.Http;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenNEL.Utils;

namespace OpenNEL_WinUI.Handlers.Plugin
{
    public class InstallPlugin
    {
        public async Task<object> Execute(string infoJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(infoJson);
                var info = doc.RootElement;
                var pluginEl = info.TryGetProperty("plugin", out var pel) ? pel : default;
                if (pluginEl.ValueKind != JsonValueKind.Object) return new { type = "install_plugin_error", message = "参数错误" };
                var id = pluginEl.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                var name = pluginEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
                var version = pluginEl.TryGetProperty("version", out var verEl) ? verEl.GetString() : null;
                var downloadUrl = pluginEl.TryGetProperty("downloadUrl", out var urlEl) ? urlEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(downloadUrl) || string.IsNullOrWhiteSpace(id)) return new { type = "install_plugin_error", message = "参数错误" };
                Log.Information("安装插件 {PluginId} {PluginName} {PluginVersion}", id, name, version);
                var http = new HttpClient();
                var bytes = await http.GetByteArrayAsync(downloadUrl);
                var dir = FileUtil.GetPluginDirectory();
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
                try { PluginManager.Instance.LoadPlugins(dir); } catch { }
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
                Log.Error(ex, "安装插件失败");
                return new { type = "install_plugin_error", message = ex.Message };
            }
        }
    }
}
