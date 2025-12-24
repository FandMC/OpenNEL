using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Utils;
using DotNetty.Buffers;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;

namespace OpenNEL.SDK.Handlers;

public class ClientHandler(GameConnection connection) : ChannelHandlerAdapter()
{
	public override void ChannelActive(IChannelHandlerContext context)
	{
		((IAttributeMap)context.Channel).GetAttribute<GameConnection>(ChannelAttribute.Connection).Set(connection);
	}

	public override void ChannelRead(IChannelHandlerContext context, object message)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		connection.OnServerReceived((IByteBuffer)message);
	}

	public override void ChannelInactive(IChannelHandlerContext context)
	{
		((IAttributeMap)context.Channel).GetAttribute<GameConnection>(ChannelAttribute.Connection).Remove();
	}
}
