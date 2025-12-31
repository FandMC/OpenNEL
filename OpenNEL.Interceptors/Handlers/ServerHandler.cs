// OpenNEL - Open Source NetEase Launcher
// Copyright (C) 2025 OpenNEL Contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using DotNetty.Buffers;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Entities;
using OpenNEL.SDK.Utils;

namespace OpenNEL.Interceptors.Handlers;

public sealed class ServerHandler(
    Interceptor interceptor,
    EntitySocks5 socks5,
    string modInfo,
    string gameId,
    string forwardAddress,
    int forwardPort,
    string nickName,
    string userId,
    string userToken,
    Action<string>? onJoinServer) : ChannelHandlerAdapter
{
    public override void ChannelActive(IChannelHandlerContext context)
    {
        var channel = context.Channel;
        interceptor.ActiveChannels.TryAdd(channel.Id, channel);

        var connection = new GameConnection(
            socks5, modInfo, gameId,
            forwardAddress, forwardPort, nickName,
            userId, userToken, channel, onJoinServer)
        {
            InterceptorId = interceptor.Identifier
        };

        ((IAttributeMap)channel)
            .GetAttribute<GameConnection>(ChannelAttribute.Connection)
            .Set(connection);

        connection.Prepare();
    }

    public override void ChannelRead(IChannelHandlerContext context, object message)
    {
        var channel = context.Channel;
        var connection = ((IAttributeMap)channel)
            .GetAttribute<GameConnection>(ChannelAttribute.Connection)
            .Get();

        connection.OnClientReceived((IByteBuffer)message);
    }

    public override void ChannelInactive(IChannelHandlerContext context)
    {
        var channel = context.Channel;
        interceptor.ActiveChannels.TryRemove(channel.Id, out _);

        var connection = ((IAttributeMap)channel)
            .GetAttribute<GameConnection>(ChannelAttribute.Connection)
            .Get();

        connection.Shutdown();
    }
}
