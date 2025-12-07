using System;
using System.Linq;
using System.Text.Json;
using Serilog;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace OpenNEL_WinUI.Handlers.Plugin
{
    public class ListAvailablePlugins
    {
        public async Task<object> Execute(string url = null)
        {
            try
            {
                var u = Environment.GetEnvironmentVariable("NEL_PLUGIN_LIST_URL");
                if (string.IsNullOrWhiteSpace(u)) u = string.IsNullOrWhiteSpace(url) ? "https://api.opennel.top/v1/get/pluginlist" : url;
                using var http = new HttpClient();
                http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var text = await http.GetStringAsync(u);
                using var doc = JsonDocument.Parse(text);
                var itemsArr = GetArray(doc.RootElement);
                var list = itemsArr.Select(Normalize).Where(x => x != null).ToArray();
                return new { type = "available_plugins", items = list! };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取插件列表失败");
                return new { type = "available_plugins", items = Array.Empty<object>() };
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
                    if (root.TryGetProperty(k, out var el) && el.ValueKind == JsonValueKind.Array)
                    {
                        return el.EnumerateArray().ToArray();
                    }
                }
            }
            return Array.Empty<JsonElement>();
        }

        private static object? Normalize(JsonElement el)
        {
            if (el.ValueKind != JsonValueKind.Object) return null;
            var id = FirstString(el, "id", "identifier", "pluginId", "pid");
            var name = FirstString(el, "name", "pluginName", "title");
            var version = FirstString(el, "version", "ver");
            var logoUrl = FirstString(el, "logoUrl", "logo", "icon", "image");
            var shortDescription = FirstString(el, "shortDescription", "description", "desc");
            var publisher = FirstString(el, "publisher", "author", "vendor");
            var downloadUrl = FirstString(el, "downloadUrl", "url", "link", "href");
            return new
            {
                id = (id ?? string.Empty).ToUpperInvariant(),
                name = name ?? string.Empty,
                version = version ?? string.Empty,
                logoUrl = (logoUrl ?? string.Empty).Replace("`", string.Empty).Trim(),
                shortDescription = shortDescription ?? string.Empty,
                publisher = publisher ?? string.Empty,
                downloadUrl = (downloadUrl ?? string.Empty).Replace("`", string.Empty).Trim()
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
