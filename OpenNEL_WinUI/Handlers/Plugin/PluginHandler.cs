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
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Codexus.Development.SDK.Manager;
using OpenNEL_WinUI.Utils;
using OpenNEL_WinUI.type;

namespace OpenNEL_WinUI.Handlers.Plugin
{
    public static class PluginHandler
    {

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
            var plugins = await new ListAvailablePlugins().Execute(AppInfo.ApiBaseURL + "/v1/pluginlist");
            var defaultIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "36D701B3-6E98-3E92-AF53-C4EC327B3A71",
                "F110DA9F-F0CB-F926-C72C-FEAC7FCF3601"
            };
            foreach (var p in plugins.Where(x => defaultIds.Contains(x.Id)))
            {
                await new InstallPlugin().Execute(p);
            }
        }

        public static async Task InstallBase1200Async()
        {
            var plugins = await new ListAvailablePlugins().Execute(AppInfo.ApiBaseURL + "/v1/pluginlist");
            var p = plugins.FirstOrDefault(x => string.Equals(x.Id, "36D701B3-6E98-3E92-AF53-C4EC327B3A71", System.StringComparison.OrdinalIgnoreCase));
            if (p != null) await new InstallPlugin().Execute(p);
        }
    }
}
