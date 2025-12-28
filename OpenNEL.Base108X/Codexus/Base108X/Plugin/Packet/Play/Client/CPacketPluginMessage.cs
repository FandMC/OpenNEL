using Codexus.Base108X.Plugin.Event;
using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Enums;
using OpenNEL.SDK.Extensions;
using OpenNEL.SDK.Manager;
using OpenNEL.SDK.Packet;
using DotNetty.Buffers;

namespace Codexus.Base108X.Plugin.Packet.Play.Client;

[RegisterPacket(EnumConnectionState.Play, EnumPacketDirection.ServerBound, 0x17, EnumProtocolVersion.V108X, false)]
public class CPacketPluginMessage : IPacket
{
	public string Identifier { get; set; } = string.Empty;

	public IByteBuffer Payload { get; set; } = Unpooled.Empty;

	public EnumProtocolVersion ClientProtocolVersion { get; set; }

	public void ReadFromBuffer(IByteBuffer buffer)
	{
		Identifier = NettyExtensions.ReadStringFromBuffer(buffer, 32767);
		Payload = Unpooled.WrappedBuffer(NettyExtensions.ReadByteArrayReadableBytes(buffer));
	}

	public void WriteToBuffer(IByteBuffer buffer)
	{
		NettyExtensions.WriteStringToBuffer(buffer, Identifier, 32);
		buffer.WriteBytes(Payload);
	}

	public bool HandlePacket(GameConnection connection)
	{
		return ((EventArgsBase)EventManager.Instance.TriggerEvent<EventPluginMessage>("base_108x", new EventPluginMessage(connection, (EnumPacketDirection)0, Identifier, Payload))).IsCancelled;
	}
}
