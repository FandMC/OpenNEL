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
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using OpenNEL.SDK.Manager;
using OpenNEL_WinUI.Utils;

namespace OpenNEL_WinUI.Handlers.Plugin
{
    public static class PluginHandler
    {
        public static List<PluginViewModel> GetInstalledPlugins()
        {
            return new ListInstalledPlugins().Execute();
        }

        public static void UninstallPlugin(string pluginId)
        {
            new UninstallPlugin().Execute(pluginId);
        }

        public static void RestartGateway()
        {
            new RestartGateway().Execute();
        }

        public static object InstallPluginByInfo(string infoJson)
        {
            return new InstallPlugin().Execute(infoJson).GetAwaiter().GetResult();
        }

        public static object UpdatePluginByInfo(string pluginId, string oldVersion, string infoJson)
        {
            return new UpdatePlugin().Execute(pluginId, oldVersion, infoJson).GetAwaiter().GetResult();
        }

        public static object ListAvailablePlugins(string url = null)
        {
            return new ListAvailablePlugins().Execute(url).GetAwaiter().GetResult();
        }

        public static (bool hasBase1200, bool hasHeypixel) DetectDefaultProtocolsInstalled()
        {
            bool hasBase = false;
            bool hasHp = false;
            foreach (var p in PluginManager.Instance.Plugins.Values)
            {
                var id = p.Id ?? string.Empty;
                var name = p.Name ?? string.Empty;
                if (string.Equals(id, "36d701b3-6e98-3e92-af53-c4ec327b3a71", System.StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Base1200", System.StringComparison.OrdinalIgnoreCase)) hasBase = true;
                if (string.Equals(id, "f110da9f-f0cb-f926-c72c-feac7fcf3601", System.StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Heypixel Protocol", System.StringComparison.OrdinalIgnoreCase)) hasHp = true;
            }
            var dir = FileUtil.GetPluginDirectory();
            try { System.IO.Directory.CreateDirectory(dir); } catch { }
            var fileBase = System.IO.File.Exists(System.IO.Path.Combine(dir, "Base1200.UG"));
            var fileHp = System.IO.File.Exists(System.IO.Path.Combine(dir, "HeypixelProtocol.UG"));
            hasBase = hasBase || fileBase;
            hasHp = hasHp || fileHp;
            return (hasBase, hasHp);
        }

        public static async Task InstallDefaultProtocolsAsync()
        {
            var basePayload = JsonSerializer.Serialize(new
            {
                plugin = new
                {
                    id = "36d701b3-6e98-3e92-af53-c4ec327b3a71",
                    name = "Base1200",
                    version = "1.4.6",
                    downloadUrl = "https://api.fandmc.cn/v2/downloads/Base1200.UG",
                    depends = ""
                }
            });
            await Task.Run(async () => await new InstallPlugin().Execute(basePayload));
            var hpPayload = JsonSerializer.Serialize(new
            {
                plugin = new
                {
                    id = "f110da9f-f0cb-f926-c72c-feac7fcf3601",
                    name = "Heypixel Protocol",
                    version = "2.2.6",
                    downloadUrl = "https://api.fandmc.cn/v2/downloads/HeypixelProtocol.UG",
                    depends = ""
                }
            });
            await Task.Run(async () => await new InstallPlugin().Execute(hpPayload));
        }

        public static async Task InstallBase1200Async()
        {
            var basePayload = JsonSerializer.Serialize(new
            {
                plugin = new
                {
                    id = "36d701b3-6e98-3e92-af53-c4ec327b3a71",
                    name = "Base1200",
                    version = "1.4.6",
                    downloadUrl = "https://api.fandmc.cn/v2/downloads/Base1200.UG",
                    depends = ""
                }
            });
            await Task.Run(async () => await new InstallPlugin().Execute(basePayload));
        }
    }
}
