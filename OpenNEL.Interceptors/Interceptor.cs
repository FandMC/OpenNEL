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
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Common.Concurrency;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using OpenNEL.Interceptors.Handlers;
using OpenNEL.SDK.Analysis;
using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Entities;
using OpenNEL.SDK.Event;
using OpenNEL.SDK.Manager;
using OpenNEL.SDK.Utils;
using Serilog;

namespace OpenNEL.Interceptors;

public sealed class Interceptor
{
    private const string MulticastAddress = "224.0.2.60";
    private const int MulticastPort = 4445;
    private const int DefaultLocalPort = 6445;
    private const int MaxPort = 35565;
    private const int BufferSize = 1048576;

    private readonly IEventLoopGroup _acceptorGroup;
    private readonly IEventLoopGroup _workerGroup;
    private IChannel? _boundChannel;
    private UdpBroadcaster? _broadcaster;

    public readonly ConcurrentDictionary<IChannelId, IChannel> ActiveChannels;

    public Guid Identifier { get; }

    public string LocalAddress { get; set; }

    public int LocalPort { get; set; }

    public string NickName { get; set; }

    public string ForwardAddress { get; private set; }

    public int ForwardPort { get; private set; }

    public string ServerName { get; set; }

    public string ServerVersion { get; set; }

    public Interceptor(
        MultithreadEventLoopGroup acceptorGroup,
        MultithreadEventLoopGroup workerGroup,
        string serverName,
        string serverVersion,
        string forwardAddress,
        int forwardPort,
        string localAddress,
        int localPort,
        string nickName)
    {
        _acceptorGroup = acceptorGroup;
        _workerGroup = workerGroup;
        ActiveChannels = new ConcurrentDictionary<IChannelId, IChannel>();
        Identifier = Guid.NewGuid();
        LocalAddress = localAddress;
        LocalPort = localPort;
        NickName = nickName;
        ForwardAddress = forwardAddress;
        ForwardPort = forwardPort;
        ServerName = serverName;
        ServerVersion = serverVersion;
    }

    public static Interceptor CreateInterceptor(
        EntitySocks5 socks5,
        string modInfo,
        string gameId,
        string serverName,
        string serverVersion,
        string forwardAddress,
        int forwardPort,
        string nickName,
        string userId,
        string userToken,
        Action<string>? onJoinServer = null,
        string localAddress = "127.0.0.1",
        int localPort = DefaultLocalPort)
    {
        var createEvent = EventManager.Instance.TriggerEvent(
            "channel_interceptor",
            new EventCreateInterceptor(localPort));

        if (createEvent.IsCancelled)
        {
            throw new InvalidOperationException("Create Interceptor cancelled");
        }

        var port = NetworkUtil.GetAvailablePort(createEvent.Port, MaxPort, reuseTimeWait: true);
        var acceptor = new MultithreadEventLoopGroup();
        var worker = new MultithreadEventLoopGroup();

        var interceptor = new Interceptor(
            acceptor, worker,
            serverName, serverVersion,
            forwardAddress, forwardPort,
            localAddress, port, nickName);

        var bootstrap = ConfigureBootstrap(acceptor, worker, interceptor, socks5, modInfo, gameId,
            forwardAddress, forwardPort, nickName, userId, userToken, onJoinServer);

        bootstrap.LocalAddress(IPAddress.Any, port);

        Log.Information("请通过{Address}游玩", $"{localAddress}:{port}");
        Log.Information("您的名字:{Name}", nickName);

        interceptor._broadcaster = CreateBroadcaster(port, forwardAddress, nickName, serverVersion);

        ((AbstractBootstrap<ServerBootstrap, IServerChannel>)(object)bootstrap)
            .BindAsync()
            .ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    interceptor._boundChannel = task.Result;
                }
            })
            .ContinueWith(_ => interceptor._broadcaster.StartBroadcastingAsync());

        return interceptor;
    }

    private static ServerBootstrap ConfigureBootstrap(
        MultithreadEventLoopGroup acceptor,
        MultithreadEventLoopGroup worker,
        Interceptor interceptor,
        EntitySocks5 socks5,
        string modInfo,
        string gameId,
        string forwardAddress,
        int forwardPort,
        string nickName,
        string userId,
        string userToken,
        Action<string>? onJoinServer)
    {
        return new ServerBootstrap()
            .Group(acceptor, worker)
            .Channel<TcpServerSocketChannel>()
            .Option(ChannelOption.SoReuseaddr, true)
            .Option(ChannelOption.SoReuseport, true)
            .Option(ChannelOption.TcpNodelay, true)
            .Option(ChannelOption.SoKeepalive, true)
            .Option(ChannelOption.Allocator, PooledByteBufferAllocator.Default)
            .Option(ChannelOption.SoSndbuf, BufferSize)
            .Option(ChannelOption.SoRcvbuf, BufferSize)
            .Option(ChannelOption.WriteBufferHighWaterMark, BufferSize)
            .Option(ChannelOption.ConnectTimeout, TimeSpan.FromSeconds(10))
            .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
            {
                channel.Pipeline
                    .AddLast("splitter", new MessageDeserializer21Bit())
                    .AddLast("handler", new ServerHandler(
                        interceptor, socks5, modInfo, gameId,
                        forwardAddress, forwardPort, nickName,
                        userId, userToken, onJoinServer))
                    .AddLast("pre-encoder", new MessageSerializer21Bit())
                    .AddLast("encoder", new MessageSerializer());
            }));
    }

    private static UdpBroadcaster CreateBroadcaster(
        int localPort, string forwardAddress, string nickName, string serverVersion)
    {
        var isLegacyVersion = serverVersion.Contains("1.8.") || serverVersion.Contains("1.7.");
        return new UdpBroadcaster(
            MulticastAddress, MulticastPort,
            localPort, forwardAddress,
            nickName, isLegacyVersion);
    }

    public async Task ChangeForwardAddressAsync(string newAddress, int newPort)
    {
        ForwardAddress = newAddress;
        ForwardPort = newPort;

        _broadcaster?.Stop();
        _broadcaster = CreateBroadcaster(LocalPort, ForwardAddress, NickName, ServerVersion);
        await _broadcaster.StartBroadcastingAsync();
    }

    public void ShutdownAsync()
    {
        try
        {
            _broadcaster?.Stop();

            foreach (var (_, channel) in ActiveChannels)
            {
                var connection = ((IAttributeMap)channel)
                    .GetAttribute<GameConnection>(ChannelAttribute.Connection).Get();
                ((IAttributeMap)channel)
                    .GetAttribute<GameConnection>(ChannelAttribute.Connection).Remove();
                connection.Shutdown();
            }

            _boundChannel?.CloseAsync();
            ((AbstractEventExecutorGroup)_acceptorGroup).ShutdownGracefullyAsync();
            ((AbstractEventExecutorGroup)_workerGroup).ShutdownGracefullyAsync();
        }
        catch
        {
        }
    }

    public static void EnsureLoaded()
    {
        var assembly = typeof(Interceptor).Assembly;
        var assemblyName = assembly.GetName().Name
            ?? throw new InvalidOperationException("Assembly name is null");

        PacketManager.Instance.RegisterPacketFromAssembly(assembly);
        Log.Debug("[Interceptor] Registered packets from {Assembly}", assemblyName);
    }
}
