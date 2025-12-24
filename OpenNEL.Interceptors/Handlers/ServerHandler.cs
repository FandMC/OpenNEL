using System;
using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Entities;
using OpenNEL.SDK.Utils;
using DotNetty.Buffers;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;

namespace OpenNEL.Interceptors.Handlers;

public class ServerHandler(Interceptor interceptor, EntitySocks5 socks5, string modInfo, string gameId, string forwardAddress, int forwardPort, string nickName, string userId, string userToken, Action<string>? onJoinServer) : ChannelHandlerAdapter()
{
	public override void ChannelActive(IChannelHandlerContext context)
	{
		interceptor.ActiveChannels.TryAdd(context.Channel.Id, context.Channel);
		IChannel channel = context.Channel;
		GameConnection gameConnection = new GameConnection(socks5, modInfo, gameId, forwardAddress, forwardPort, nickName, userId, userToken, channel, onJoinServer);
		((IAttributeMap)channel).GetAttribute<GameConnection>(ChannelAttribute.Connection).Set(gameConnection);
		gameConnection.Prepare();
	}

	public override void ChannelRead(IChannelHandlerContext context, object message)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		((IAttributeMap)context.Channel).GetAttribute<GameConnection>(ChannelAttribute.Connection).Get().OnClientReceived((IByteBuffer)message);
	}

	public override void ChannelInactive(IChannelHandlerContext context)
	{
		interceptor.ActiveChannels.TryRemove(context.Channel.Id, out var _);
		((IAttributeMap)context.Channel).GetAttribute<GameConnection>(ChannelAttribute.Connection).Get().Shutdown();
	}
}
