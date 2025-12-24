using OpenNEL.SDK.Entities;

namespace OpenNEL.SDK.Manager;

public interface IUserManager
{
	static IUserManager? Instance;

	static IUserManager? CppInstance;

	EntityAvailableUser? GetAvailableUser(string entityId);
}
