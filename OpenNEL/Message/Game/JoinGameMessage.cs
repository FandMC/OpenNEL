using OpenNEL.Network;
using OpenNEL.type;
using OpenNEL.Manager;
using OpenNEL.Manager;
using System.Text.Json;

namespace OpenNEL.Message.Game;

internal class JoinGameMessage : IWsMessage
{
    public string Type => "join_game";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var serverId = FirstString(root, "serverId", "gameId");
        var serverName = FirstString(root, "serverName", "gameName") ?? string.Empty;
        var role = FirstString(root, "role", "roleId", "roleName", "name");
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null) return new { type = "notlogin" };
        if (string.IsNullOrWhiteSpace(serverId) || string.IsNullOrWhiteSpace(role))
        {
            return new { type = "start_error", message = "参数错误" };
        }
        try
        {
            var ok = await GameManager.Instance.StartAsync(serverId!, serverName, role!);
            if (!ok) return new { type = "start_error", message = "启动失败" };
            return new { type = "channels_updated" };
        }
        catch (System.Exception ex)
        {
            Serilog.Log.Error(ex, "启动失败");
            return new { type = "start_error", message = "启动失败" };
        }
    }

    static string? FirstString(JsonElement root, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (root.TryGetProperty(k, out var v))
            {
                if (v.ValueKind == JsonValueKind.String) return v.GetString();
                if (v.ValueKind == JsonValueKind.Number) return v.ToString();
            }
        }
        return null;
    }
}
