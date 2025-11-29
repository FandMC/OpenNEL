using OpenNEL.Network;

using System.Text.Json;
using OpenNEL.Manager;

namespace OpenNEL.Message.Game;

internal class ShutdownGameMessage : IWsMessage
{
    public string Type => "shutdown_game";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var closed = new List<string>();
        if (root.TryGetProperty("identifiers", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in arr.EnumerateArray())
            {
                var s = el.GetString();
                if (string.IsNullOrWhiteSpace(s)) continue;
                if (Guid.TryParse(s, out var id))
                {
                    GameManager.Instance.ShutdownInterceptor(id);
                    closed.Add(s);
                }
            }
        }
        else
        {
            Serilog.Log.Warning("shutdown_game 请求缺少 identifiers，已忽略关闭操作");
        }
        var payloads = new object[]
        {
            new { type = "shutdown_ack", identifiers = closed.ToArray() },
            new { type = "channels_updated" }
        };
        return payloads;
    }
}
