using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Manager;

namespace OpenNEL.SDK.Event;

public class EventEncryptionRequest : EventArgsBase
{
	public string ServerId { get; }

	public EventEncryptionRequest(GameConnection connection, string serverId)
		: base(connection)
	{
		ServerId = serverId;
	}
}
