using OpenNEL.Network;
using OpenNEL.Manager;
using System.Text.Json;
using Serilog;
namespace OpenNEL.Message.Login;

internal class DeleteAccountMessage : IWsMessage
{
    public string Type => "delete_account";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var id = root.TryGetProperty("entityId", out var idProp) ? idProp.GetString() : null;
        if (string.IsNullOrWhiteSpace(id))
        {
            return new { type = "delete_error", message = "entityId为空" };
        }
        UserManager.Instance.RemoveAvailableUser(id);
        UserManager.Instance.RemoveUser(id);
        var users = UserManager.Instance.GetUsersNoDetails();
        var items = users.Select(u => new { entityId = u.UserId, channel = u.Channel }).ToArray();
        return new { type = "accounts", items };
    }
}
