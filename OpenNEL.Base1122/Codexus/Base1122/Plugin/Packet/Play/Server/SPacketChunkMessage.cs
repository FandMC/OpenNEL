using System;
using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Enums;
using OpenNEL.SDK.Extensions;
using OpenNEL.SDK.Packet;
using DotNetty.Buffers;

namespace Codexus.Base1122.Plugin.Packet.Play.Server;

[RegisterPacket(EnumConnectionState.Play, EnumPacketDirection.ClientBound, 0x20, EnumProtocolVersion.V1122, true)]
public class SPacketChunkMessage : IPacket
{
	public int ChunkX { get; set; }

	public int ChunkZ { get; set; }

	public bool LoadChunk { get; set; }

	public int AvailableSections { get; set; }

	public byte[] Buffer { get; set; } = Array.Empty<byte>();

	public EnumProtocolVersion ClientProtocolVersion { get; set; }

	public void ReadFromBuffer(IByteBuffer buffer)
	{
		ChunkX = buffer.ReadInt();
		ChunkZ = buffer.ReadInt();
		LoadChunk = buffer.ReadBoolean();
		AvailableSections = NettyExtensions.ReadVarIntFromBuffer(buffer);
		int num = NettyExtensions.ReadVarIntFromBuffer(buffer);
		Buffer = new byte[num];
		buffer.ReadBytes(Buffer);
	}

	public void WriteToBuffer(IByteBuffer buffer)
	{
		buffer.WriteInt(ChunkX);
		buffer.WriteInt(ChunkZ);
		buffer.WriteBoolean(LoadChunk);
		NettyExtensions.WriteVarInt(buffer, AvailableSections);
		NettyExtensions.WriteVarInt(buffer, Buffer.Length);
		buffer.WriteBytes(Buffer);
		NettyExtensions.WriteVarInt(buffer, 0);
	}

	public bool HandlePacket(GameConnection connection)
	{
		return false;
	}
}
