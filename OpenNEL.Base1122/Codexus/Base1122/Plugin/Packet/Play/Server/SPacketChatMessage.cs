using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Enums;
using OpenNEL.SDK.Extensions;
using OpenNEL.SDK.Packet;
using DotNetty.Buffers;

namespace Codexus.Base1122.Plugin.Packet.Play.Server;

[RegisterPacket(EnumConnectionState.Play, EnumPacketDirection.ClientBound, 0x0F, EnumProtocolVersion.V1122, true)]
public class SPacketChatMessage : IPacket
{
	public string Json { get; set; } = string.Empty;

	public byte Position { get; set; }

	public EnumProtocolVersion ClientProtocolVersion { get; set; }

	public void ReadFromBuffer(IByteBuffer buffer)
	{
		Json = NettyExtensions.ReadStringFromBuffer(buffer, 32767);
		Position = buffer.ReadByte();
	}

	public void WriteToBuffer(IByteBuffer buffer)
	{
		NettyExtensions.WriteStringToBuffer(buffer, Json, 32767);
		buffer.WriteByte((int)Position);
	}

	public bool HandlePacket(GameConnection connection)
	{
		return false;
	}
}
