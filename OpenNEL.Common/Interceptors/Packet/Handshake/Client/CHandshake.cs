using System;
using System.Text;
using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Enums;
using OpenNEL.SDK.Extensions;
using OpenNEL.SDK.Manager;
using OpenNEL.SDK.Packet;
using OpenNEL.SDK.Utils;
using OpenNEL.Common.Interceptors.Event;
using DotNetty.Buffers;
using Serilog;

namespace OpenNEL.Common.Interceptors.Packet.Handshake.Client;

[RegisterPacket(EnumConnectionState.Handshaking, EnumPacketDirection.ServerBound, 0, false)]
public class CHandshake : IPacket
{
	public int ProtocolVersion { get; set; }

	public string ServerAddress { get; set; } = "";

	public ushort ServerPort { get; set; }

	public EnumConnectionState NextState { get; set; }

	public EnumProtocolVersion ClientProtocolVersion { get; set; }

	public void ReadFromBuffer(IByteBuffer buffer)
	{
		ProtocolVersion = buffer.ReadVarIntFromBuffer();
		ServerAddress = buffer.ReadStringFromBuffer(255);
		ServerPort = buffer.ReadUnsignedShort();
		NextState = (EnumConnectionState)buffer.ReadVarIntFromBuffer();
	}

	public void WriteToBuffer(IByteBuffer buffer)
	{
		buffer.WriteVarInt(ProtocolVersion);
		buffer.WriteStringToBuffer(ServerAddress);
		buffer.WriteShort((int)ServerPort);
		buffer.WriteVarInt((int)NextState);
	}

	public bool HandlePacket(GameConnection connection)
	{
		if (EventManager.Instance.TriggerEvent(MessageChannels.AllVersions, new EventHandshake(connection, this)).IsCancelled)
		{
			return true;
		}
		connection.ProtocolVersion = (EnumProtocolVersion)ProtocolVersion;
		connection.State = NextState;
		Log.Debug("Original address: {ServerAddress}", new object[1] { Convert.ToBase64String(Encoding.UTF8.GetBytes(ServerAddress)) });
		ServerPort = (ushort)connection.ForwardPort;
		EnumProtocolVersion protocolVersion = connection.ProtocolVersion;
		string serverAddress = ((protocolVersion <= EnumProtocolVersion.V1180) ? ((protocolVersion <= EnumProtocolVersion.V1122) ? (connection.ForwardAddress + "\0FML\0") : (connection.ForwardAddress + "\0FML2\0")) : ((protocolVersion <= EnumProtocolVersion.V1206) ? (connection.ForwardAddress + "\0FML3\0") : (connection.ForwardAddress + "\0FORGE")));
		ServerAddress = serverAddress;
		Log.Debug("New address: {ServerAddress}", new object[1] { Convert.ToBase64String(Encoding.UTF8.GetBytes(ServerAddress)) });
		Log.Debug("Protocol version: {ProtocolVersion}, Next state: {State}, Address: {Address}", new object[3]
		{
			connection.ProtocolVersion,
			connection.State,
			ServerAddress.Replace("\0", "|")
		});
		return false;
	}
}
