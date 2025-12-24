using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Enums;
using OpenNEL.SDK.Event;
using OpenNEL.SDK.Extensions;
using OpenNEL.SDK.Manager;
using OpenNEL.SDK.Packet;
using OpenNEL.Common.Interceptors.Packet.Login.Client;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Security;
using Serilog;

namespace OpenNEL.Common.Interceptors.Packet.Login.Server;

[RegisterPacket(EnumConnectionState.Login, EnumPacketDirection.ClientBound, 1, new EnumProtocolVersion[]
{
	EnumProtocolVersion.V1076,
	EnumProtocolVersion.V108X,
	EnumProtocolVersion.V1200,
	EnumProtocolVersion.V1122,
	EnumProtocolVersion.V1180,
	EnumProtocolVersion.V1210,
	EnumProtocolVersion.V1206
}, false)]
public class SPacketEncryptionRequest : IPacket
{
	private string ServerId { get; set; } = "";

	private byte[] PublicKey { get; set; } = Array.Empty<byte>();

	private byte[] VerifyToken { get; set; } = Array.Empty<byte>();

	private bool ShouldAuthenticate { get; set; }

	public EnumProtocolVersion ClientProtocolVersion { get; set; }

	public void ReadFromBuffer(IByteBuffer buffer)
	{
		if (ClientProtocolVersion == EnumProtocolVersion.V1076)
		{
			ServerId = buffer.ReadStringFromBuffer(20);
			PublicKey = buffer.ReadByteArrayFromBuffer(buffer.ReadShort());
			VerifyToken = buffer.ReadByteArrayFromBuffer(buffer.ReadShort());
			return;
		}
		ServerId = buffer.ReadStringFromBuffer(20);
		PublicKey = buffer.ReadByteArrayFromBuffer();
		VerifyToken = buffer.ReadByteArrayFromBuffer();
		EnumProtocolVersion clientProtocolVersion = ClientProtocolVersion;
		if ((uint)(clientProtocolVersion - 766) <= 1u)
		{
			ShouldAuthenticate = buffer.ReadBoolean();
		}
	}

	public void WriteToBuffer(IByteBuffer buffer)
	{
		if (ClientProtocolVersion == EnumProtocolVersion.V1076)
		{
			buffer.WriteStringToBuffer(ServerId).WriteShort(PublicKey.Length).WriteBytes(PublicKey)
				.WriteShort(VerifyToken.Length)
				.WriteBytes(VerifyToken);
			return;
		}
		buffer.WriteStringToBuffer(ServerId).WriteByteArrayToBuffer(PublicKey).WriteByteArrayToBuffer(VerifyToken);
		if (ClientProtocolVersion == EnumProtocolVersion.V1210)
		{
			buffer.WriteBoolean(ShouldAuthenticate);
		}
	}

	public bool HandlePacket(GameConnection connection)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Expected O, but got Unknown
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Expected O, but got Unknown
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Expected O, but got Unknown
		CipherKeyGenerator val = new CipherKeyGenerator();
		val.Init(new KeyGenerationParameters(new SecureRandom(), 128));
		SubjectPublicKeyInfo instance = SubjectPublicKeyInfo.GetInstance((object)PublicKey);
		byte[] secretKey = val.GenerateKey();
		using MemoryStream memoryStream = new MemoryStream(20);
		memoryStream.Write(Encoding.GetEncoding("ISO-8859-1").GetBytes(ServerId));
		memoryStream.Write(secretKey);
		memoryStream.Write(PublicKey);
		memoryStream.Position = 0L;
		string text = memoryStream.ToSha1();
		if (!EventManager.Instance.TriggerEvent("channel_interceptor", new EventEncryptionRequest(connection, text)).IsCancelled)
		{
			connection.OnJoinServer?.Invoke(text);
		}
		Pkcs1Encoding val2 = new Pkcs1Encoding((IAsymmetricBlockCipher)new RsaEngine());
		val2.Init(true, (ICipherParameters)PublicKeyFactory.CreateKey(instance));
		CPacketEncryptionResponse cPacketEncryptionResponse = new CPacketEncryptionResponse
		{
			ClientProtocolVersion = ClientProtocolVersion,
			SecretKeyEncrypted = val2.ProcessBlock(secretKey, 0, secretKey.Length),
			VerifyTokenEncrypted = val2.ProcessBlock(VerifyToken, 0, VerifyToken.Length)
		};
		if (connection.ServerChannel == null)
		{
			Log.Error("Server channel is null", Array.Empty<object>());
			return false;
		}
		connection.ServerChannel.Configuration.AutoRead = false;
		connection.ServerChannel.Configuration.SetOption<bool>(ChannelOption.AutoRead, false);
		connection.ServerChannel.WriteAndFlushAsync((object)cPacketEncryptionResponse).ContinueWith(delegate(Task channel)
		{
			if (!channel.IsCompletedSuccessfully)
			{
				return;
			}
			try
			{
				Log.Debug("Successfully sent encryption response to client", Array.Empty<object>());
				GameConnection.EnableEncryption(connection.ServerChannel, secretKey);
				connection.ServerChannel.Configuration.AutoRead = true;
				connection.ServerChannel.Configuration.SetOption<bool>(ChannelOption.AutoRead, true);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to enable encryption", Array.Empty<object>());
			}
		});
		return true;
	}
}
