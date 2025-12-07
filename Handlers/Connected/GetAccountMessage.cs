using System.Linq;
using OpenNEL.Manager;

namespace OpenNEL.Message.Connected;

public class GetAccountMessage
{
    public object Execute(string entityId)
    {
        var users = UserManager.Instance.GetUsersNoDetails();
        var items = users.Select(u => new { entityId = u.UserId, channel = u.Channel, status = u.Authorized ? "online" : "offline" }).ToArray();
        return new { type = "accounts", items };
    }
}
