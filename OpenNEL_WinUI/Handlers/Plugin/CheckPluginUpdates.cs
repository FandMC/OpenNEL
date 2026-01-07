/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;

namespace OpenNEL_WinUI.Handlers.Plugin
{
    public class PluginUpdateInfo
    {
        public string Id { get; set; } = string.Empty;
        public string CurrentVersion { get; set; } = string.Empty;
        public string LatestVersion { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public bool HasUpdate { get; set; }
    }

    public class CheckPluginUpdates
    {
        public async Task<Dictionary<string, PluginUpdateInfo>> Execute(IEnumerable<PluginViewModel> installedPlugins)
        {
            var result = new Dictionary<string, PluginUpdateInfo>(StringComparer.OrdinalIgnoreCase);
            
            try
            {
                var availablePlugins = await new ListAvailablePlugins().Execute();
                var availableDict = new Dictionary<string, AvailablePluginItem>(StringComparer.OrdinalIgnoreCase);
                
                foreach (var p in availablePlugins)
                {
                    if (!string.IsNullOrEmpty(p.Id))
                    {
                        availableDict[p.Id] = p;
                    }
                }

                foreach (var installed in installedPlugins)
                {
                    var info = new PluginUpdateInfo
                    {
                        Id = installed.Id,
                        CurrentVersion = installed.Version ?? "0.0.0",
                        HasUpdate = false
                    };

                    if (availableDict.TryGetValue(installed.Id, out var available))
                    {
                        info.LatestVersion = available.Version ?? "0.0.0";
                        info.DownloadUrl = available.DownloadUrl ?? string.Empty;
                        info.HasUpdate = CompareVersions(info.CurrentVersion, info.LatestVersion) < 0;
                    }

                    result[installed.Id] = info;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "检测插件更新失败");
            }

            return result;
        }

        private static int CompareVersions(string current, string latest)
        {
            try
            {
                var c = ParseVersion(current);
                var l = ParseVersion(latest);
                
                for (int i = 0; i < Math.Max(c.Length, l.Length); i++)
                {
                    var cv = i < c.Length ? c[i] : 0;
                    var lv = i < l.Length ? l[i] : 0;
                    if (cv < lv) return -1;
                    if (cv > lv) return 1;
                }
                return 0;
            }
            catch
            {
                return string.Compare(current, latest, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static int[] ParseVersion(string version)
        {
            var parts = version.Split('.', '-', '_');
            var result = new List<int>();
            foreach (var part in parts)
            {
                if (int.TryParse(part, out var num))
                {
                    result.Add(num);
                }
            }
            return result.Count > 0 ? result.ToArray() : new[] { 0 };
        }
    }
}
