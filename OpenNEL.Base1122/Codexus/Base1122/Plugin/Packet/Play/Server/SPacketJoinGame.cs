using Codexus.Base1122.Plugin.Event;
using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Enums;
using OpenNEL.SDK.Extensions;
using OpenNEL.SDK.Manager;
using OpenNEL.SDK.Packet;
using DotNetty.Buffers;

namespace Codexus.Base1122.Plugin.Packet.Play.Server;

[RegisterPacket(EnumConnectionState.Play, EnumPacketDirection.ClientBound, 0x23, EnumProtocolVersion.V1122, false)]
public class SPacketJoinGame : IPacket
{
	private int EntityId { get; set; }

	private byte GameMode { get; set; }

	private int Dimension { get; set; }

	private byte Difficulty { get; set; }

	private byte MaxPlayers { get; set; }

	private string LevelType { get; set; } = string.Empty;

	private bool ReducedDebugInfo { get; set; }

	public EnumProtocolVersion ClientProtocolVersion { get; set; }

	public void ReadFromBuffer(IByteBuffer buffer)
	{
		EntityId = buffer.ReadInt();
		GameMode = buffer.ReadByte();
		Dimension = buffer.ReadInt();
		Difficulty = buffer.ReadByte();
		MaxPlayers = buffer.ReadByte();
		LevelType = NettyExtensions.ReadStringFromBuffer(buffer, 32);
		ReducedDebugInfo = buffer.ReadBoolean();
	}

	public void WriteToBuffer(IByteBuffer buffer)
	{
		buffer.WriteInt(EntityId);
		buffer.WriteByte((int)(sbyte)GameMode);
		buffer.WriteInt(Dimension);
		buffer.WriteByte((int)(sbyte)Difficulty);
		buffer.WriteByte((int)(sbyte)MaxPlayers);
		NettyExtensions.WriteStringToBuffer(buffer, LevelType, 32767);
		buffer.WriteBoolean(ReducedDebugInfo);
	}

	public bool HandlePacket(GameConnection connection)
	{
		return ((EventArgsBase)EventManager.Instance.TriggerEvent<EventJoinGame>("base_1122", new EventJoinGame(connection, this))).IsCancelled;
	}
}
