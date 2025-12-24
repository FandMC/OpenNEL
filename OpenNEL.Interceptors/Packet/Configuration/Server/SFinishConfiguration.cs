using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Enums;
using OpenNEL.SDK.Packet;
using DotNetty.Buffers;

namespace OpenNEL.Interceptors.Packet.Configuration.Server;

[RegisterPacket(EnumConnectionState.Configuration, EnumPacketDirection.ClientBound, 3, new EnumProtocolVersion[]
{
	EnumProtocolVersion.V1206,
	EnumProtocolVersion.V1210
}, false)]
public class SFinishConfiguration : IPacket
{
	public EnumProtocolVersion ClientProtocolVersion { get; set; }

	public void ReadFromBuffer(IByteBuffer buffer)
	{
	}

	public void WriteToBuffer(IByteBuffer buffer)
	{
	}

	public bool HandlePacket(GameConnection connection)
	{
		return false;
	}
}
