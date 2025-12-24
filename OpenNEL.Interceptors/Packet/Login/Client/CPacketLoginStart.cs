using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Enums;
using OpenNEL.SDK.Extensions;
using OpenNEL.SDK.Packet;
using DotNetty.Buffers;
using Serilog;

namespace OpenNEL.Interceptors.Packet.Login.Client;

[RegisterPacket(EnumConnectionState.Login, EnumPacketDirection.ServerBound, 0, new EnumProtocolVersion[]
{
	EnumProtocolVersion.V1076,
	EnumProtocolVersion.V108X,
	EnumProtocolVersion.V1200,
	EnumProtocolVersion.V1122,
	EnumProtocolVersion.V1180,
	EnumProtocolVersion.V1210,
	EnumProtocolVersion.V1206
}, false)]
public class CPacketLoginStart : IPacket
{
	private string Profile { get; set; } = "";

	private bool HasPlayerUuid { get; set; }

	private byte[] Uuid { get; } = new byte[16];

	public EnumProtocolVersion ClientProtocolVersion { get; set; }

	public void ReadFromBuffer(IByteBuffer buffer)
	{
		switch (ClientProtocolVersion)
		{
		case EnumProtocolVersion.V1206:
		case EnumProtocolVersion.V1210:
			Profile = buffer.ReadStringFromBuffer(16);
			buffer.ReadBytes(Uuid);
			break;
		case EnumProtocolVersion.V1200:
			Profile = buffer.ReadStringFromBuffer(16);
			HasPlayerUuid = buffer.ReadBoolean();
			if (HasPlayerUuid)
			{
				buffer.ReadBytes(Uuid);
			}
			break;
		case EnumProtocolVersion.V1076:
		case EnumProtocolVersion.V108X:
		case EnumProtocolVersion.V1122:
		case EnumProtocolVersion.V1180:
			Profile = buffer.ReadStringFromBuffer(16);
			break;
		}
	}

	public void WriteToBuffer(IByteBuffer buffer)
	{
		switch (ClientProtocolVersion)
		{
		case EnumProtocolVersion.V1206:
		case EnumProtocolVersion.V1210:
			buffer.WriteStringToBuffer(Profile, 16);
			buffer.WriteBytes(Uuid);
			break;
		case EnumProtocolVersion.V1200:
			buffer.WriteStringToBuffer(Profile, 16);
			buffer.WriteBoolean(HasPlayerUuid);
			if (HasPlayerUuid)
			{
				buffer.WriteBytes(Uuid);
			}
			break;
		case EnumProtocolVersion.V1180:
			buffer.WriteStringToBuffer(Profile);
			break;
		case EnumProtocolVersion.V1076:
		case EnumProtocolVersion.V108X:
		case EnumProtocolVersion.V1122:
			buffer.WriteStringToBuffer(Profile, 16);
			break;
		}
	}

	public bool HandlePacket(GameConnection connection)
	{
		Profile = connection.NickName;
		Log.Debug("{NickName} trying to login...", new object[1] { Profile });
		return false;
	}
}
