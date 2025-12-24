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
using System.Linq;
using OpenNEL.PluginLoader.Manager;
using OpenNEL_WinUI.type;
using Serilog;

namespace OpenNEL_WinUI.Handlers.Plugin
{
    public class UninstallPlugin
    {
        public object Execute(string pluginId)
        {
            if (!string.IsNullOrWhiteSpace(pluginId))
            {
                Log.Information("卸载插件 {PluginId}", pluginId);
                PluginManager.Instance.UninstallPlugin(pluginId);
                AppState.WaitRestartPlugins[pluginId] = true;
            }
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
    }
}
