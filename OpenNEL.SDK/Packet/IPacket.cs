using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Enums;
using DotNetty.Buffers;

namespace OpenNEL.SDK.Packet;

public interface IPacket
{
	EnumProtocolVersion ClientProtocolVersion { get; set; }

	void ReadFromBuffer(IByteBuffer buffer);

	void WriteToBuffer(IByteBuffer buffer);

	bool HandlePacket(GameConnection connection);
}
