using Codexus.Base1122.Plugin.Packet.Play.Client;
using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Manager;

namespace Codexus.Base1122.Plugin.Event;

public class EventPlayerPosition : EventArgsBase
{
	public CPacketPlayerPosition Packet { get; }

	public EventPlayerPosition(GameConnection connection, CPacketPlayerPosition packet)
		:base(connection)
	{
		Packet = packet;
	}
}
