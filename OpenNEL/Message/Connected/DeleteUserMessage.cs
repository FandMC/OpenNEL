using System.Text.Json;
using OpenNEL.Entities;
using OpenNEL.Manager;
using OpenNEL.Network;

namespace OpenNEL.Message.Connected;

internal class DeleteUserMessage : IWsMessage
{
    public string Type => "delete_user";

    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var id = root.TryGetProperty("id", out var v) ? v.ToString() : string.Empty;
        if (string.IsNullOrWhiteSpace(id)) return null;
        UserManager.Instance.RemoveUser(id);
        UserManager.Instance.RemoveAvailableUser(id);
        var users = UserManager.Instance.GetUsersNoDetails();
        var items = users.Select(u => new { entityId = u.UserId, channel = u.Channel, status = u.Authorized ? "online" : "offline" }).ToArray();
        return new { type = "accounts", items };
    }
}
