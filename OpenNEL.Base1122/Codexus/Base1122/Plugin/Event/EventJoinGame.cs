using Codexus.Base1122.Plugin.Packet.Play.Server;
using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Manager;

namespace Codexus.Base1122.Plugin.Event;

public class EventJoinGame : EventArgsBase
{
	public SPacketJoinGame Packet { get; }

	public EventJoinGame(GameConnection connection, SPacketJoinGame packet)
		:base(connection)
	{
		Packet = packet;
	}
}
