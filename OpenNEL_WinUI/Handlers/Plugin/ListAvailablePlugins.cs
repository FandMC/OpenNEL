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
using Serilog;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using OpenNEL_WinUI.type;

namespace OpenNEL_WinUI.Handlers.Plugin
{
    public class ListAvailablePlugins
    {
        private static readonly HttpClient Http = new();

        public async Task<List<AvailablePluginItem>> Execute(string? url = null)
        {
            try
            {
                var u = string.IsNullOrWhiteSpace(url) ? AppInfo.ApiBaseURL + "/v1/pluginlist" : url;
                Http.DefaultRequestHeaders.Accept.Clear();
                Http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var text = await Http.GetStringAsync(u).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(text);
                var itemsArr = GetArray(doc.RootElement);
                return itemsArr.Select(Normalize).Where(x => x != null).ToList()!;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取插件列表失败");
                return new List<AvailablePluginItem>();
            }
        }

        private static JsonElement[] GetArray(JsonElement root)
        {
            if (root.ValueKind == JsonValueKind.Array)
            {
                return root.EnumerateArray().ToArray();
            }
            if (root.ValueKind == JsonValueKind.Object)
            {
                var keys = new[] { "items", "data", "plugins", "list" };
                foreach (var k in keys)
                {
                    if (root.TryGetProperty(k, out var el))
                    {
                        if (el.ValueKind == JsonValueKind.Array)
                            return el.EnumerateArray().ToArray();
                        if (k == "plugins" && el.ValueKind == JsonValueKind.Object)
                        {
                            if (el.TryGetProperty("items", out var inner) && inner.ValueKind == JsonValueKind.Array)
                                return inner.EnumerateArray().ToArray();
                        }
                    }
                }
            }
            return Array.Empty<JsonElement>();
        }

        private static AvailablePluginItem? Normalize(JsonElement el)
        {
            if (el.ValueKind != JsonValueKind.Object) return null;
            var id = FirstString(el, "id", "identifier", "pluginId", "pid");
            var name = FirstString(el, "name", "pluginName", "title");
            var version = FirstString(el, "version", "ver");
            var logoUrl = FirstString(el, "logoUrl", "logo", "icon", "image");
            var shortDescription = FirstString(el, "shortDescription", "description", "desc");
            var publisher = FirstString(el, "publisher", "author", "vendor");
            var downloadUrl = FirstString(el, "downloadUrl", "url", "link", "href");
            var depends = FirstString(el, "depends", "dependency", "dep");
            return new AvailablePluginItem
            {
                Id = (id ?? string.Empty).ToUpperInvariant(),
                Name = name ?? string.Empty,
                Version = version ?? string.Empty,
                LogoUrl = (logoUrl ?? string.Empty).Replace("`", string.Empty).Trim(),
                ShortDescription = shortDescription ?? string.Empty,
                Publisher = publisher ?? string.Empty,
                DownloadUrl = (downloadUrl ?? string.Empty).Replace("`", string.Empty).Trim(),
                Depends = (depends ?? string.Empty).ToUpperInvariant()
            };
        }

        private static string? FirstString(JsonElement el, params string[] keys)
        {
            foreach (var k in keys)
            {
                if (el.TryGetProperty(k, out var v))
                {
                    if (v.ValueKind == JsonValueKind.String) return v.GetString();
                    if (v.ValueKind == JsonValueKind.Number) return v.ToString();
                    if (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False) return v.ToString();
                    if (v.ValueKind == JsonValueKind.Object || v.ValueKind == JsonValueKind.Array)
                    {
                        try { return v.ToString(); } catch { }
                    }
                }
            }
            return null;
        }
    }
}
