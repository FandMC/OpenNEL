using System.Linq;
using OpenNEL.Manager;

namespace OpenNEL_WinUI.Handlers.Login
{
    public class DeleteAccount
    {
        public object Execute(string entityId)
        {
            if (string.IsNullOrWhiteSpace(entityId))
            {
                return new { type = "delete_error", message = "entityId为空" };
            }
            UserManager.Instance.RemoveAvailableUser(entityId);
            UserManager.Instance.RemoveUser(entityId);
            var users = UserManager.Instance.GetUsersNoDetails();
            var items = users.Select(u => new { entityId = u.UserId, channel = u.Channel }).ToArray();
            return new { type = "accounts", items };
        }
    }
}
