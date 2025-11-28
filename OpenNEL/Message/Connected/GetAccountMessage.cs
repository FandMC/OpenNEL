using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using OpenNEL.Entities;
using OpenNEL.Entities.Web;
using OpenNEL.Manager;
using OpenNEL.Network;

namespace OpenNEL.Message.Connected;

public class GetAccountMessage : IWsMessage
{
    public string Type => "list_accounts";

    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var users = UserManager.Instance.GetUsersNoDetails();
        var items = users.Select(u => new { entityId = u.UserId, channel = u.Channel, status = u.Authorized ? "online" : "offline" }).ToArray();
        return new { type = "accounts", items };
    }
}
