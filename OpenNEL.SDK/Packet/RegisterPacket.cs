using System;
using OpenNEL.SDK.Enums;

namespace OpenNEL.SDK.Packet;

[AttributeUsage(AttributeTargets.Class)]
public class RegisterPacket(EnumConnectionState state, EnumPacketDirection direction, int[] packetIds, EnumProtocolVersion[] versions, bool skip = false) : Attribute()
{
	public EnumConnectionState State { get; } = state;

	public EnumPacketDirection Direction { get; } = direction;

	public int[] PacketIds { get; } = packetIds;

	public EnumProtocolVersion[] Versions { get; } = versions;

	public bool Skip { get; } = skip;

	public RegisterPacket(EnumConnectionState state, EnumPacketDirection direction, int packetId, bool skip = false)
		: this(state, direction, new int[1] { packetId }, Enum.GetValues<EnumProtocolVersion>(), skip)
	{
	}

	public RegisterPacket(EnumConnectionState state, EnumPacketDirection direction, int[] packetIds, bool skip = false)
		: this(state, direction, packetIds, Enum.GetValues<EnumProtocolVersion>(), skip)
	{
	}

	public RegisterPacket(EnumConnectionState state, EnumPacketDirection direction, int packetId, EnumProtocolVersion version, bool skip = false)
		: this(state, direction, new int[1] { packetId }, new EnumProtocolVersion[1] { version }, skip)
	{
	}

	public RegisterPacket(EnumConnectionState state, EnumPacketDirection direction, int[] packetIds, EnumProtocolVersion version, bool skip = false)
		: this(state, direction, packetIds, new EnumProtocolVersion[1] { version }, skip)
	{
	}

	public RegisterPacket(EnumConnectionState state, EnumPacketDirection direction, int packetId, EnumProtocolVersion[] versions, bool skip = false)
		: this(state, direction, new int[1] { packetId }, versions, skip)
	{
	}
}
