using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using OpenNEL.SDK.Analysis;
using OpenNEL.SDK.Entities;
using OpenNEL.SDK.Enums;
using OpenNEL.SDK.Event;
using OpenNEL.SDK.Extensions;
using OpenNEL.SDK.Handlers;
using OpenNEL.SDK.Manager;
using OpenNEL.SDK.Packet;
using OpenNEL.SDK.Utils;
using DotNetty.Buffers;
using DotNetty.Common.Concurrency;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using OpenTl.Netty.Socks.Handlers;
using Serilog;

namespace OpenNEL.SDK.Connection;

public class GameConnection : IConnection
{
	public readonly IChannel ClientChannel;

	private bool _initialized;

	private MultithreadEventLoopGroup? _workerGroup;

	public IChannel? ServerChannel;

	private readonly EntitySocks5 _socks5;

	public string NickName { get; set; }

	public EnumProtocolVersion ProtocolVersion { get; set; }

	public EnumConnectionState State { get; set; }

	public Action<string>? OnJoinServer { get; set; }

	public MultithreadEventLoopGroup TaskGroup { get; }

	public GameSession Session { get; set; }

	public string GameId { get; }

	public string ModInfo { get; }

	public int ForwardPort { get; }

	public string ForwardAddress { get; }

	public byte[] Uuid { get; set; }

	public GameConnection(EntitySocks5 socks5, string modInfo, string gameId, string forwardAddress, int forwardPort, string nickName, string userId, string userToken, IChannel channel, Action<string>? onJoinServer)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected O, but got Unknown
		_socks5 = socks5;
		ClientChannel = channel;
		NickName = nickName;
		ProtocolVersion = EnumProtocolVersion.None;
		OnJoinServer = onJoinServer;
		TaskGroup = new MultithreadEventLoopGroup();
		Session = new GameSession(nickName, userId, userToken);
		GameId = gameId;
		ModInfo = modInfo;
		ForwardPort = forwardPort;
		ForwardAddress = forwardAddress;
		Uuid = new byte[16];
	}

	public void Prepare()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected O, but got Unknown
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Expected O, but got Unknown
		_initialized = false;
		if (_workerGroup != null)
		{
			Shutdown();
		}
		_workerGroup = new MultithreadEventLoopGroup();
		Bootstrap bootstrap = ((AbstractBootstrap<Bootstrap, IChannel>)(object)((AbstractBootstrap<Bootstrap, IChannel>)(object)((AbstractBootstrap<Bootstrap, IChannel>)(object)((AbstractBootstrap<Bootstrap, IChannel>)(object)((AbstractBootstrap<Bootstrap, IChannel>)(object)((AbstractBootstrap<Bootstrap, IChannel>)(object)((AbstractBootstrap<Bootstrap, IChannel>)(object)((AbstractBootstrap<Bootstrap, IChannel>)(object)((AbstractBootstrap<Bootstrap, IChannel>)(object)((AbstractBootstrap<Bootstrap, IChannel>)new Bootstrap()).Group((IEventLoopGroup)(object)_workerGroup)).Channel<TcpSocketChannel>()).Option<bool>(ChannelOption.TcpNodelay, true)).Option<bool>(ChannelOption.SoKeepalive, true)).Option<IByteBufferAllocator>(ChannelOption.Allocator, (IByteBufferAllocator)PooledByteBufferAllocator.Default)).Option<int>(ChannelOption.SoSndbuf, 1048576)).Option<int>(ChannelOption.SoRcvbuf, 1048576)).Option<int>(ChannelOption.WriteBufferHighWaterMark, 1048576)).Option<TimeSpan>(ChannelOption.ConnectTimeout, TimeSpan.FromSeconds(30.0))).Handler((IChannelHandler)(object)new ActionChannelInitializer<IChannel>((Action<IChannel>)delegate(IChannel channel)
		{
			//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Expected O, but got Unknown
			if (_socks5.Enabled)
			{
				if (!IPAddress.TryParse(_socks5.Address, out IPAddress address))
				{
					address = Dns.GetHostAddressesAsync(_socks5.Address).GetAwaiter().GetResult()
						.First();
				}
				channel.Pipeline.AddLast("socks5", (IChannelHandler)new Socks5ProxyHandler((EndPoint)new IPEndPoint(address, _socks5.Port), _socks5.Username, _socks5.Password));
			}
			channel.Pipeline.AddLast("splitter", (IChannelHandler)(object)new MessageDeserializer21Bit());
			channel.Pipeline.AddLast("handler", (IChannelHandler)(object)new ClientHandler(this)).AddLast("pre-encoder", (IChannelHandler)(object)new MessageSerializer21Bit()).AddLast("encoder", (IChannelHandler)(object)new MessageSerializer());
		}));
		Task.Run(async delegate
		{
			EventParseAddress finalAddress = EventManager.Instance.TriggerEvent("channel_connection", new EventParseAddress(this, ForwardAddress, ForwardPort));
			IPAddress address;
			IChannel serverChannel = await (IPAddress.TryParse(finalAddress.Address, out address) ? bootstrap.ConnectAsync(address, finalAddress.Port) : bootstrap.ConnectAsync(finalAddress.Address, finalAddress.Port)).ContinueWith((Func<Task<IChannel>, IChannel>)delegate(Task<IChannel> channel)
			{
				if (!channel.IsFaulted)
				{
					return channel.Result;
				}
				Log.Error((Exception)channel.Exception, "Failed to connect to remote server {Address}:{Port}", new object[2] { finalAddress.Address, finalAddress.Port });
				return (IChannel)null;
			});
			ServerChannel = serverChannel;
			_initialized = true;
		});
		while (!_initialized)
		{
			Thread.Sleep(100);
		}
		if (ServerChannel == null)
		{
			Shutdown();
		}
	}

	public void OnServerReceived(IByteBuffer buffer)
	{
		HandlePacketReceived(buffer, EnumPacketDirection.ClientBound, delegate(object data)
		{
			ClientChannel.WriteAndFlushAsync(data);
		});
	}

	public void OnClientReceived(IByteBuffer buffer)
	{
		HandlePacketReceived(buffer, EnumPacketDirection.ServerBound, delegate(object data)
		{
			IChannel? serverChannel = ServerChannel;
			if (serverChannel != null)
			{
				serverChannel.WriteAndFlushAsync(data);
			}
		});
	}

	public void Shutdown()
	{
		EventManager.Instance.TriggerEvent("channel_connection", new EventConnectionClosed(this));
		Log.Debug("Shutting down connection...", Array.Empty<object>());
		((AbstractEventExecutorGroup)TaskGroup).ShutdownGracefullyAsync();
		ClientChannel.CloseAsync();
		IChannel? serverChannel = ServerChannel;
		if (serverChannel != null)
		{
			serverChannel.CloseAsync();
		}
		MultithreadEventLoopGroup workerGroup = _workerGroup;
		if (workerGroup != null)
		{
			((AbstractEventExecutorGroup)workerGroup).ShutdownGracefullyAsync();
		}
	}

	private void HandlePacketReceived(IByteBuffer buffer, EnumPacketDirection direction, Action<object> onRedirect)
	{
		buffer.MarkReaderIndex();
		int num = buffer.ReadVarIntFromBuffer();
		IPacket packet = PacketManager.Instance.BuildPacket(State, direction, ProtocolVersion, num);
		if (packet == null)
		{
			buffer.ResetReaderIndex();
			onRedirect(buffer);
			return;
		}
		RegisterPacket metadata = PacketManager.Instance.GetMetadata(packet);
		if (metadata != null && metadata.Skip)
		{
			buffer.ResetReaderIndex();
			onRedirect(buffer);
			return;
		}
		packet.ClientProtocolVersion = ProtocolVersion;
		try
		{
			packet.ReadFromBuffer(buffer);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Cannot read packet from buffer, direction: {Direction}, Id: {Id}, Packet: {Packet}, ProtocolVersion: {ProtocolVersion}", new object[4] { direction, num, packet, ProtocolVersion });
			throw;
		}
		if (packet.HandlePacket(this))
		{
			buffer.ResetReaderIndex();
			return;
		}
		buffer.ResetReaderIndex();
		onRedirect(packet);
	}

	public static void EnableCompression(IChannel channel, int threshold)
	{
		if (threshold < 0)
		{
			if (channel.Pipeline.Get("decompress") is NettyCompressionDecoder)
			{
				channel.Pipeline.Remove("decompress");
			}
			if (channel.Pipeline.Get("compress") is NettyCompressionEncoder)
			{
				channel.Pipeline.Remove("compress");
			}
		}
		else
		{
			if (channel.Pipeline.Get("decompress") is NettyCompressionDecoder nettyCompressionDecoder)
			{
				nettyCompressionDecoder.Threshold = threshold;
			}
			else
			{
				channel.Pipeline.AddAfter("splitter", "decompress", (IChannelHandler)(object)new NettyCompressionDecoder(threshold));
			}
			if (channel.Pipeline.Get("compress") is NettyCompressionEncoder nettyCompressionEncoder)
			{
				nettyCompressionEncoder.Threshold = threshold;
			}
			else
			{
				channel.Pipeline.AddBefore("encoder", "compress", (IChannelHandler)(object)new NettyCompressionEncoder(threshold));
			}
		}
	}

	public static void EnableEncryption(IChannel channel, byte[] secretKey)
	{
		channel.Pipeline.AddBefore("splitter", "decrypt", (IChannelHandler)(object)new NettyEncryptionDecoder(secretKey)).AddBefore("pre-encoder", "encrypt", (IChannelHandler)(object)new NettyEncryptionEncoder(secretKey));
	}
}
