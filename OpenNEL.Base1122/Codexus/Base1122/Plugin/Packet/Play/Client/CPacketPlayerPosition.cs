using Codexus.Base1122.Plugin.Event;
using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Enums;
using OpenNEL.SDK.Manager;
using OpenNEL.SDK.Packet;
using DotNetty.Buffers;

namespace Codexus.Base1122.Plugin.Packet.Play.Client;

[RegisterPacket(EnumConnectionState.Play, EnumPacketDirection.ServerBound, 0x0E, EnumProtocolVersion.V1122, false)]
public class CPacketPlayerPosition : IPacket
{
	public double X { get; private set; }

	private double Y { get; set; }

	public double Z { get; private set; }

	private float Yaw { get; set; }

	private float Pitch { get; set; }

	private bool OnGround { get; set; }

	public EnumProtocolVersion ClientProtocolVersion { get; set; }

	public void ReadFromBuffer(IByteBuffer buffer)
	{
		X = buffer.ReadDouble();
		Y = buffer.ReadDouble();
		Z = buffer.ReadDouble();
		Yaw = buffer.ReadFloat();
		Pitch = buffer.ReadFloat();
		OnGround = buffer.ReadBoolean();
	}

	public void WriteToBuffer(IByteBuffer buffer)
	{
		buffer.WriteDouble(X);
		buffer.WriteDouble(Y);
		buffer.WriteDouble(Z);
		buffer.WriteFloat(Yaw);
		buffer.WriteFloat(Pitch);
		buffer.WriteBoolean(OnGround);
	}

	public bool HandlePacket(GameConnection connection)
	{
		return ((EventArgsBase)EventManager.Instance.TriggerEvent<EventPlayerPosition>("base_1122", new EventPlayerPosition(connection, this))).IsCancelled;
	}
}
