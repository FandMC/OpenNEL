using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using OpenNEL.SDK.Analysis;
using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Entities;
using OpenNEL.SDK.Event;
using OpenNEL.SDK.Manager;
using OpenNEL.SDK.Utils;
using OpenNEL.Common.Interceptors.Handlers;
using DotNetty.Buffers;
using DotNetty.Common.Concurrency;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Serilog;

namespace OpenNEL.Common.Interceptors;

public class Interceptor
{
	public readonly ConcurrentDictionary<IChannelId, IChannel> ActiveChannels;

	private IEventLoopGroup acceptorGroup;

	private IEventLoopGroup workerGroup;

	public Guid Identifier { get; }

	public string LocalAddress { get; set; }

	public int LocalPort { get; set; }

	public string NickName { get; set; }

	public string ForwardAddress { get; private set; }

	public int ForwardPort { get; private set; }

	public string ServerName { get; set; }

	public string ServerVersion { get; set; }

	private IChannel? Channel { get; set; }

	private UdpBroadcaster? UdpBroadcaster { get; set; }

	public Interceptor(MultithreadEventLoopGroup acceptorGroup, MultithreadEventLoopGroup workerGroup, string serverName, string serverVersion, string forwardAddress, int forwardPort, string localAddress, int localPort, string nickName)
	{
		this.acceptorGroup = (IEventLoopGroup)(object)acceptorGroup;
		this.workerGroup = (IEventLoopGroup)(object)workerGroup;
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

	public static Interceptor CreateInterceptor(EntitySocks5 socks5, string modInfo, string gameId, string serverName, string serverVersion, string forwardAddress, int forwardPort, string nickName, string userId, string userToken, Action<string>? onJoinServer = null, string localAddress = "127.0.0.1", int localPort = 6445)
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Expected O, but got Unknown
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Expected O, but got Unknown
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Expected O, but got Unknown
		EventCreateInterceptor eventCreateInterceptor = EventManager.Instance.TriggerEvent("channel_interceptor", new EventCreateInterceptor(localPort));
		if (eventCreateInterceptor.IsCancelled)
		{
			throw new InvalidOperationException("Create Interceptor cancelled");
		}
		int availablePort = NetworkUtil.GetAvailablePort(eventCreateInterceptor.Port, 35565, reuseTimeWait: true);
		MultithreadEventLoopGroup val = new MultithreadEventLoopGroup();
		MultithreadEventLoopGroup val2 = new MultithreadEventLoopGroup();
		Interceptor interceptor = new Interceptor(val, val2, serverName, serverVersion, forwardAddress, forwardPort, localAddress, availablePort, nickName);
		ServerBootstrap val3 = ((AbstractBootstrap<ServerBootstrap, IServerChannel>)(object)((AbstractBootstrap<ServerBootstrap, IServerChannel>)(object)((AbstractBootstrap<ServerBootstrap, IServerChannel>)(object)((AbstractBootstrap<ServerBootstrap, IServerChannel>)(object)((AbstractBootstrap<ServerBootstrap, IServerChannel>)(object)((AbstractBootstrap<ServerBootstrap, IServerChannel>)(object)((AbstractBootstrap<ServerBootstrap, IServerChannel>)(object)((AbstractBootstrap<ServerBootstrap, IServerChannel>)(object)((AbstractBootstrap<ServerBootstrap, IServerChannel>)(object)((AbstractBootstrap<ServerBootstrap, IServerChannel>)(object)((AbstractBootstrap<ServerBootstrap, IServerChannel>)(object)new ServerBootstrap().Group((IEventLoopGroup)(object)val, (IEventLoopGroup)(object)val2)).Channel<TcpServerSocketChannel>()).Option<bool>(ChannelOption.SoReuseaddr, true)).Option<bool>(ChannelOption.SoReuseport, true)).Option<bool>(ChannelOption.TcpNodelay, true)).Option<bool>(ChannelOption.SoKeepalive, true)).Option<IByteBufferAllocator>(ChannelOption.Allocator, (IByteBufferAllocator)PooledByteBufferAllocator.Default)).Option<int>(ChannelOption.SoSndbuf, 1048576)).Option<int>(ChannelOption.SoRcvbuf, 1048576)).Option<int>(ChannelOption.WriteBufferHighWaterMark, 1048576)).Option<TimeSpan>(ChannelOption.ConnectTimeout, TimeSpan.FromSeconds(10.0)).ChildHandler((IChannelHandler)(object)new ActionChannelInitializer<IChannel>((Action<IChannel>)delegate(IChannel channel)
		{
			channel.Pipeline.AddLast("splitter", (IChannelHandler)(object)new MessageDeserializer21Bit()).AddLast("handler", (IChannelHandler)(object)new ServerHandler(interceptor, socks5, modInfo, gameId, forwardAddress, forwardPort, nickName, userId, userToken, onJoinServer)).AddLast("pre-encoder", (IChannelHandler)(object)new MessageSerializer21Bit())
				.AddLast("encoder", (IChannelHandler)(object)new MessageSerializer());
		}))).LocalAddress(IPAddress.Any, availablePort);
		Log.Information("请通过{Address}游玩", new object[1] { $"{localAddress}:{availablePort}" });
		Log.Information("您的名字:{Name}", new object[1] { nickName });
		interceptor.UdpBroadcaster = new UdpBroadcaster("224.0.2.60", 4445, availablePort, forwardAddress, nickName, serverVersion.Contains("1.8.") || serverVersion.Contains("1.7."));
		((AbstractBootstrap<ServerBootstrap, IServerChannel>)(object)val3).BindAsync().ContinueWith(delegate(Task<IChannel> task)
		{
			if (task.IsCompletedSuccessfully)
			{
				interceptor.Channel = task.Result;
			}
		}).ContinueWith((Task _) => interceptor.UdpBroadcaster.StartBroadcastingAsync());
		return interceptor;
	}

	public async Task ChangeForwardAddressAsync(string newAddress, int newPort)
	{
		ForwardAddress = newAddress;
		ForwardPort = newPort;
		UdpBroadcaster?.Stop();
		UdpBroadcaster = new UdpBroadcaster("224.0.2.60", 4445, LocalPort, ForwardAddress, NickName, ServerVersion.Contains("1.8.") || ServerVersion.Contains("1.7."));
		await UdpBroadcaster.StartBroadcastingAsync();
	}

	public void ShutdownAsync()
	{
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			UdpBroadcaster?.Stop();
			foreach (KeyValuePair<IChannelId, IChannel> activeChannel in ActiveChannels)
			{
				activeChannel.Deconstruct(out var _, out var value);
				IChannel val = value;
				GameConnection gameConnection = ((IAttributeMap)val).GetAttribute<GameConnection>(ChannelAttribute.Connection).Get();
				((IAttributeMap)val).GetAttribute<GameConnection>(ChannelAttribute.Connection).Remove();
				gameConnection.Shutdown();
			}
			IChannel? channel = Channel;
			if (channel != null)
			{
				channel.CloseAsync();
			}
			((AbstractEventExecutorGroup)acceptorGroup).ShutdownGracefullyAsync();
			((AbstractEventExecutorGroup)workerGroup).ShutdownGracefullyAsync();
		}
		catch
		{
		}
	}

	public static void EnsureLoaded()
	{
		if (typeof(Interceptor).Assembly.GetName().Name == null)
		{
			throw new InvalidOperationException("Should never call CheckIsLoaded()");
		}
	}
}
