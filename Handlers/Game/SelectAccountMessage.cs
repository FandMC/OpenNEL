using System;
using OpenNEL.type;
using OpenNEL.Manager;

namespace OpenNEL_WinUI.Handlers.Game;

public class SelectAccount
{
    public object Execute(string entityId)
    {
        if (string.IsNullOrWhiteSpace(entityId)) return new { type = "notlogin" };
        var available = UserManager.Instance.GetAvailableUser(entityId);
        if (available == null) return new { type = "notlogin" };
        available.LastLoginTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return new { type = "selected_account", entityId };
    }
}
