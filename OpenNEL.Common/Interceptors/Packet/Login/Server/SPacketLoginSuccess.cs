using System;
using System.Collections.Generic;
using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Entities;
using OpenNEL.SDK.Enums;
using OpenNEL.SDK.Event;
using OpenNEL.SDK.Extensions;
using OpenNEL.SDK.Manager;
using OpenNEL.SDK.Packet;
using OpenNEL.SDK.Utils;
using DotNetty.Buffers;
using Serilog;

namespace OpenNEL.Common.Interceptors.Packet.Login.Server;

[RegisterPacket(EnumConnectionState.Login, EnumPacketDirection.ClientBound, 2, new EnumProtocolVersion[]
{
	EnumProtocolVersion.V1076,
	EnumProtocolVersion.V108X,
	EnumProtocolVersion.V1200,
	EnumProtocolVersion.V1122,
	EnumProtocolVersion.V1180,
	EnumProtocolVersion.V1210,
	EnumProtocolVersion.V1206
}, false)]
public class SPacketLoginSuccess : IPacket
{
	private string Uuid { get; set; } = "";

	private byte[] Guid { get; init; } = new byte[16];

	private string Username { get; set; } = "";

	private List<Property>? Properties { get; set; }

	private bool StrictErrorHandling { get; set; }

	public EnumProtocolVersion ClientProtocolVersion { get; set; }

	public void ReadFromBuffer(IByteBuffer buffer)
	{
		switch (ClientProtocolVersion)
		{
		case EnumProtocolVersion.V1206:
		case EnumProtocolVersion.V1210:
			buffer.ReadBytes(Guid);
			Username = buffer.ReadStringFromBuffer(16);
			Properties = buffer.ReadProperties();
			StrictErrorHandling = buffer.ReadBoolean();
			break;
		case EnumProtocolVersion.V1200:
			buffer.ReadBytes(Guid);
			Username = buffer.ReadStringFromBuffer(16);
			Properties = buffer.ReadProperties();
			break;
		case EnumProtocolVersion.V1076:
		case EnumProtocolVersion.V108X:
		case EnumProtocolVersion.V1122:
		case EnumProtocolVersion.V1180:
			Uuid = buffer.ReadStringFromBuffer(36);
			Username = buffer.ReadStringFromBuffer(16);
			break;
		}
	}

	public void WriteToBuffer(IByteBuffer buffer)
	{
		switch (ClientProtocolVersion)
		{
		case EnumProtocolVersion.V1206:
		case EnumProtocolVersion.V1210:
			buffer.WriteBytes(Guid);
			buffer.WriteStringToBuffer(Username, 16);
			buffer.WriteProperties(Properties);
			buffer.WriteBoolean(StrictErrorHandling);
			break;
		case EnumProtocolVersion.V1200:
			buffer.WriteBytes(Guid);
			buffer.WriteStringToBuffer(Username, 16);
			buffer.WriteProperties(Properties);
			break;
		case EnumProtocolVersion.V1076:
		case EnumProtocolVersion.V108X:
		case EnumProtocolVersion.V1122:
		case EnumProtocolVersion.V1180:
			buffer.WriteStringToBuffer(Uuid, 36);
			buffer.WriteStringToBuffer(Username, 16);
			break;
		}
	}

	public bool HandlePacket(GameConnection connection)
	{
		switch (ClientProtocolVersion)
		{
		case EnumProtocolVersion.V1200:
		case EnumProtocolVersion.V1206:
		case EnumProtocolVersion.V1210:
			Log.Information("{0}({1}) 加入了服务器", new object[2]
			{
				Username,
				new Guid(Guid, bigEndian: true)
			});
			break;
		case EnumProtocolVersion.V1076:
		case EnumProtocolVersion.V108X:
		case EnumProtocolVersion.V1122:
		case EnumProtocolVersion.V1180:
			Log.Information("{0}({1}) 加入了服务器", new object[2] { Username, Uuid });
			break;
		}
		connection.Uuid = Guid;
		if (ClientProtocolVersion >= EnumProtocolVersion.V1206)
		{
			return false;
		}
		if (EventManager.Instance.TriggerEvent(MessageChannels.AllVersions, new EventLoginSuccess(connection)).IsCancelled)
		{
			return true;
		}
		connection.State = EnumConnectionState.Play;
		return false;
	}
}
