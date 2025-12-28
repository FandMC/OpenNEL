using Codexus.Base108X.Plugin.Event;
using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Enums;
using OpenNEL.SDK.Manager;
using OpenNEL.SDK.Packet;
using DotNetty.Buffers;

namespace Codexus.Base108X.Plugin.Packet.Play.Client;

[RegisterPacket(EnumConnectionState.Play, EnumPacketDirection.ServerBound, 0x0A, EnumProtocolVersion.V108X, false)]
public class CPacketAnimation : IPacket
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
		return ((EventArgsBase)EventManager.Instance.TriggerEvent<EventAnimation>("base_108x", new EventAnimation(connection))).IsCancelled;
	}
}
