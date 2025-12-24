using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Manager;
using OpenNEL.Common.Interceptors.Packet.Handshake.Client;

namespace OpenNEL.Common.Interceptors.Event;

public class EventHandshake : EventArgsBase
{
	public CHandshake Handshake { get; }

	public EventHandshake(GameConnection connection, CHandshake handshake)
		: base(connection)
	{
		Handshake = handshake;
	}
}
