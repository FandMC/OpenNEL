using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Manager;

namespace OpenNEL.SDK.Event;

public class EventLoginSuccess : EventArgsBase
{
	public EventLoginSuccess(GameConnection connection)
		: base(connection)
	{
	}
}
