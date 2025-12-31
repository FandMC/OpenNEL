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
using System;
using System.Text.Json;
using Codexus.Development.SDK.Manager;
using Serilog;
using System.Net.Http;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenNEL_WinUI.Utils;

namespace OpenNEL_WinUI.Handlers.Plugin
{
    public class UpdatePlugin
    {
        public async Task<object> Execute(string pluginId, string oldVersion, string infoJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(infoJson);
                var pluginEl = doc.RootElement.TryGetProperty("plugin", out var pel) ? pel : default;
                if (pluginEl.ValueKind != JsonValueKind.Object) return new { type = "update_plugin_error", message = "参数错误" };
                var name = pluginEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
                var newVersion = pluginEl.TryGetProperty("version", out var verEl) ? verEl.GetString() : null;
                var downloadUrl = pluginEl.TryGetProperty("downloadUrl", out var urlEl) ? urlEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(downloadUrl)) return new { type = "update_plugin_error", message = "参数错误" };
                Log.Information("更新插件 {PluginId} {PluginName} {OldVersion} -> {NewVersion}", pluginId, name, oldVersion, newVersion);
                var http = new HttpClient();
                var bytes = await http.GetByteArrayAsync(downloadUrl);
                var dir = FileUtil.GetPluginDirectory();
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
                    PluginManager.Instance.LoadPlugins(dir);
                }
                catch { }
                var updPayload = new { type = "installed_plugins_updated" };
                var items = PluginManager.Instance.Plugins.Values.Select(plugin => new {
                    identifier = plugin.Id,
                    name = plugin.Name,
                    version = plugin.Version,
                    description = plugin.Description,
                    author = plugin.Author,
                    status = plugin.Status
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
}
