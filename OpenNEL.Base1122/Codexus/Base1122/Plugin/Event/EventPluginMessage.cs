using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Enums;
using OpenNEL.SDK.Manager;
using DotNetty.Buffers;

namespace Codexus.Base1122.Plugin.Event;

public class EventPluginMessage : EventArgsBase
{
	public EnumPacketDirection Direction { get; set; }

	public string Identifier { get; set; }

	public IByteBuffer Payload { get; set; }

	public EventPluginMessage(GameConnection connection, EnumPacketDirection direction, string identifier, IByteBuffer payload)
		:base(connection)
	{
		Direction = direction;
		Identifier = identifier;
		Payload = payload;
	}
}
