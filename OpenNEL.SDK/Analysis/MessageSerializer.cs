using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Extensions;
using OpenNEL.SDK.Manager;
using OpenNEL.SDK.Packet;
using OpenNEL.SDK.Utils;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;

namespace OpenNEL.SDK.Analysis;

public class MessageSerializer : MessageToByteEncoder<IPacket>
{
	protected override void Encode(IChannelHandlerContext context, IPacket message, IByteBuffer output)
	{
		GameConnection gameConnection = ((IAttributeMap)context.Channel).GetAttribute<GameConnection>(ChannelAttribute.Connection).Get();
		int packetId = PacketManager.Instance.GetPacketId(gameConnection.ProtocolVersion, message);
		if (packetId != -1)
		{
			output.WriteVarInt(packetId);
			message.WriteToBuffer(output);
		}
	}
}
