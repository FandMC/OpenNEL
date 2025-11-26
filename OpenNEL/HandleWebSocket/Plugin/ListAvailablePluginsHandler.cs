using OpenNEL.network;
using System.Text;
using System.Text.Json;
using Serilog;
using System.Net.Http;

namespace OpenNEL.HandleWebSocket.Plugin;

internal class ListAvailablePluginsHandler : IWsHandler
{
    public string Type => "list_available_plugins";
    public async Task ProcessAsync(System.Net.WebSockets.WebSocket ws, JsonElement root)
    {
        try
        {
            var u = Environment.GetEnvironmentVariable("NEL_PLUGIN_LIST_URL");
            if (string.IsNullOrWhiteSpace(u)) u = "https://api.opennel.top/v1/get/pluginlist";
            using var http = new HttpClient();
            var text = await http.GetStringAsync(u);
            using var doc = JsonDocument.Parse(text);
            var arr = doc.RootElement.ValueKind == JsonValueKind.Array ? doc.RootElement : default;
            var payload = arr.ValueKind == JsonValueKind.Array ? JsonSerializer.Serialize(new { type = "available_plugins", items = arr }) : JsonSerializer.Serialize(new { type = "available_plugins", items = Array.Empty<object>() });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(payload)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取插件列表失败");
            var payload = JsonSerializer.Serialize(new { type = "available_plugins", items = Array.Empty<object>() });
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(payload)), System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
        }
    }
}