using Codexus.Base1122.Plugin.Event;
using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Enums;
using OpenNEL.SDK.Extensions;
using OpenNEL.SDK.Manager;
using OpenNEL.SDK.Packet;
using DotNetty.Buffers;

namespace Codexus.Base1122.Plugin.Packet.Play.Client;

[RegisterPacket(EnumConnectionState.Play, EnumPacketDirection.ServerBound, 0x02, EnumProtocolVersion.V1122, false)]
public class CPacketChatMessage : IPacket
{
	private string Message { get; set; } = string.Empty;

	public EnumProtocolVersion ClientProtocolVersion { get; set; }

	public void ReadFromBuffer(IByteBuffer buffer)
	{
		Message = NettyExtensions.ReadStringFromBuffer(buffer, 256);
	}

	public void WriteToBuffer(IByteBuffer buffer)
	{
		NettyExtensions.WriteStringToBuffer(buffer, Message, 32767);
	}

	public bool HandlePacket(GameConnection connection)
	{
		return ((EventArgsBase)EventManager.Instance.TriggerEvent<EventChatMessage>("base_1122", new EventChatMessage(connection, Message))).IsCancelled;
	}
}
