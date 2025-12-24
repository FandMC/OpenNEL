using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Manager;
using OpenNEL.Interceptors.Packet.Handshake.Client;

namespace OpenNEL.Interceptors.Event;

public class EventHandshake : EventArgsBase
{
	public CHandshake Handshake { get; }

	public EventHandshake(GameConnection connection, CHandshake handshake)
		: base(connection)
	{
		Handshake = handshake;
	}
}
