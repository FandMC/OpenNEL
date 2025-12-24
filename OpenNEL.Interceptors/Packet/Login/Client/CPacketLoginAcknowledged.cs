using System;
using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Enums;
using OpenNEL.SDK.Event;
using OpenNEL.SDK.Manager;
using OpenNEL.SDK.Packet;
using OpenNEL.SDK.Utils;
using DotNetty.Buffers;
using Serilog;

namespace OpenNEL.Interceptors.Packet.Login.Client;

[RegisterPacket(EnumConnectionState.Login, EnumPacketDirection.ServerBound, 3, new EnumProtocolVersion[]
{
	EnumProtocolVersion.V1210,
	EnumProtocolVersion.V1206
}, false)]
public class CPacketLoginAcknowledged : IPacket
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
		if (EventManager.Instance.TriggerEvent(MessageChannels.AllVersions, new EventLoginSuccess(connection)).IsCancelled)
		{
			return true;
		}
		connection.State = EnumConnectionState.Configuration;
		Log.Debug("Configuration started.", Array.Empty<object>());
		return false;
	}
}
