using OpenNEL.Network;
using OpenNEL.type;
using OpenNEL.Manager;

using System.Text.Json;

namespace OpenNEL.Message.Game;

internal class SelectAccountMessage : IWsMessage
{
    public string Type => "select_account";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var id = root.TryGetProperty("entityId", out var idProp2) ? idProp2.GetString() : null;
        if (string.IsNullOrWhiteSpace(id)) return new { type = "notlogin" };
        var available = UserManager.Instance.GetAvailableUser(id!);
        if (available == null) return new { type = "notlogin" };
        available.LastLoginTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return new { type = "selected_account", entityId = id };
    }
}
