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
using Serilog;
using System.Net.Http;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Codexus.Development.SDK.Manager;
using OpenNEL_WinUI.Utils;

namespace OpenNEL_WinUI.Handlers.Plugin
{
    public class InstallPluginRequest
    {
        public AvailablePluginItem? Plugin { get; set; }
    }

    public class InstallPlugin
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public Task<object> Execute(AvailablePluginItem item)
        {
            return ExecuteInternal(item.Id, item.Name, item.Version, item.DownloadUrl, item.Depends);
        }

        public async Task<object> Execute(string infoJson)
        {
            try
            {
                var req = JsonSerializer.Deserialize<InstallPluginRequest>(infoJson, JsonOptions);
                if (req?.Plugin == null) return new { type = "install_plugin_error", message = "参数错误" };
                return await Execute(req.Plugin);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "安装插件失败");
                return new { type = "install_plugin_error", message = ex.Message };
            }
        }

        private async Task<object> ExecuteInternal(string? id, string? name, string? version, string? downloadUrl, string? depends)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(downloadUrl) || string.IsNullOrWhiteSpace(id)) return new { type = "install_plugin_error", message = "参数错误" };
                if (!string.IsNullOrWhiteSpace(depends))
                {
                    var need = !PluginManager.Instance.Plugins.Values.Any(p => string.Equals(p.Id, depends, StringComparison.OrdinalIgnoreCase));
                    if (need)
                    {
                        var items = await new ListAvailablePlugins().Execute();
                        var depItem = items.FirstOrDefault(x => string.Equals(x.Id, depends, StringComparison.OrdinalIgnoreCase));
                        if (depItem == null)
                        {
                            Log.Error("依赖未找到: {Dep}", depends);
                            return new { type = "install_plugin_error", message = "依赖未找到" };
                        }
                        await Execute(depItem);
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
                var resultItems = PluginManager.Instance.Plugins.Values.Select(plugin => new {
                    identifier = plugin.Id,
                    name = plugin.Name,
                    version = plugin.Version,
                    description = plugin.Description,
                    author = plugin.Author,
                    status = plugin.Status
                }).ToArray();
                var listPayload = new { type = "installed_plugins", items = resultItems };
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
