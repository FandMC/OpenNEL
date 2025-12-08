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
                var depends = pluginEl.TryGetProperty("depends", out var depEl) ? depEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(downloadUrl) || string.IsNullOrWhiteSpace(id)) return new { type = "install_plugin_error", message = "参数错误" };
                if (!string.IsNullOrWhiteSpace(depends))
                {
                    var need = !PluginManager.Instance.Plugins.Values.Any(p => string.Equals(p.Id, depends, StringComparison.OrdinalIgnoreCase));
                    if (need)
                    {
                        var obj = await new ListAvailablePlugins().Execute();
                        var itemsProp = obj.GetType().GetProperty("items");
                        var arr = itemsProp != null ? itemsProp.GetValue(obj) as System.Array : null;
                        string depId = depends;
                        object depItemObj = null;
                        if (arr != null)
                        {
                            foreach (var it in arr)
                            {
                                var iid = GetPropString(it, "id");
                                if (!string.IsNullOrWhiteSpace(iid) && string.Equals(iid, depId, StringComparison.OrdinalIgnoreCase))
                                {
                                    depItemObj = it;
                                    break;
                                }
                            }
                        }
                        if (depItemObj == null)
                        {
                            Log.Error("依赖未找到: {Dep}", depId);
                            return new { type = "install_plugin_error", message = "依赖未找到" };
                        }
                        var depPayload = JsonSerializer.Serialize(new
                        {
                            plugin = new
                            {
                                id = GetPropString(depItemObj, "id") ?? string.Empty,
                                name = GetPropString(depItemObj, "name") ?? string.Empty,
                                version = GetPropString(depItemObj, "version") ?? string.Empty,
                                downloadUrl = GetPropString(depItemObj, "downloadUrl") ?? string.Empty,
                                depends = GetPropString(depItemObj, "depends") ?? string.Empty
                            }
                        });
                        await Execute(depPayload);
                    }
                }
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

        private static string GetPropString(object o, string name)
        {
            var p = o.GetType().GetProperty(name);
            var v = p != null ? p.GetValue(o) : null;
            return v != null ? v.ToString() : null;
        }
    }
}
