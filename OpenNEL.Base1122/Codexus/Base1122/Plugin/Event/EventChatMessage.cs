using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Manager;

namespace Codexus.Base1122.Plugin.Event;

public class EventChatMessage : EventArgsBase
{
	public string Message { get; }

	public EventChatMessage(GameConnection connection, string message)
		:base(connection)
	{
		Message = message;
	}
}
