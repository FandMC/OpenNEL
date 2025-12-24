using System;
using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Enums;
using OpenNEL.SDK.Extensions;
using OpenNEL.SDK.Packet;
using DotNetty.Buffers;

namespace OpenNEL.Common.Interceptors.Packet.Login.Client;

[RegisterPacket(EnumConnectionState.Login, EnumPacketDirection.ServerBound, 1, new EnumProtocolVersion[]
{
	EnumProtocolVersion.V1076,
	EnumProtocolVersion.V108X,
	EnumProtocolVersion.V1200,
	EnumProtocolVersion.V1122,
	EnumProtocolVersion.V1180,
	EnumProtocolVersion.V1210,
	EnumProtocolVersion.V1206
}, false)]
public class CPacketEncryptionResponse : IPacket
{
	public required byte[] SecretKeyEncrypted { get; set; } = Array.Empty<byte>();

	public required byte[] VerifyTokenEncrypted { get; set; } = Array.Empty<byte>();

	public EnumProtocolVersion ClientProtocolVersion { get; set; }

	public void ReadFromBuffer(IByteBuffer buffer)
	{
		if (ClientProtocolVersion == EnumProtocolVersion.V1076)
		{
			SecretKeyEncrypted = buffer.ReadByteArrayFromBuffer(buffer.ReadShort());
			VerifyTokenEncrypted = buffer.ReadByteArrayFromBuffer(buffer.ReadShort());
		}
		else
		{
			SecretKeyEncrypted = buffer.ReadByteArrayFromBuffer();
			VerifyTokenEncrypted = buffer.ReadByteArrayFromBuffer();
		}
	}

	public void WriteToBuffer(IByteBuffer buffer)
	{
		if (ClientProtocolVersion == EnumProtocolVersion.V1076)
		{
			buffer.WriteShort(SecretKeyEncrypted.Length).WriteBytes(SecretKeyEncrypted).WriteShort(VerifyTokenEncrypted.Length)
				.WriteBytes(VerifyTokenEncrypted);
		}
		else
		{
			buffer.WriteByteArrayToBuffer(SecretKeyEncrypted).WriteByteArrayToBuffer(VerifyTokenEncrypted);
		}
	}

	public bool HandlePacket(GameConnection connection)
	{
		return false;
	}
}
