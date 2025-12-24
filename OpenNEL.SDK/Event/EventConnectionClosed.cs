using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Manager;

namespace OpenNEL.SDK.Event;

public class EventConnectionClosed : EventArgsBase
{
	public EventConnectionClosed(GameConnection connection)
		: base(connection)
	{
	}
}
