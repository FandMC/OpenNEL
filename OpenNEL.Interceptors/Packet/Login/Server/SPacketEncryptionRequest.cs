// OpenNEL - Open Source NetEase Launcher
// Copyright (C) 2025 OpenNEL Contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using OpenNEL.Interceptors.Packet.Login.Client;
using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Enums;
using OpenNEL.SDK.Event;
using OpenNEL.SDK.Extensions;
using OpenNEL.SDK.Manager;
using OpenNEL.SDK.Packet;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Security;
using Serilog;

namespace OpenNEL.Interceptors.Packet.Login.Server;

[RegisterPacket(
    EnumConnectionState.Login,
    EnumPacketDirection.ClientBound,
    1,
    [EnumProtocolVersion.V1076, EnumProtocolVersion.V108X, EnumProtocolVersion.V1200,
     EnumProtocolVersion.V1122, EnumProtocolVersion.V1180, EnumProtocolVersion.V1210,
     EnumProtocolVersion.V1206],
    false)]
public sealed class SPacketEncryptionRequest : IPacket
{
    private const int MaxServerIdLength = 20;
    private const int KeySize = 128;

    private string ServerId { get; set; } = string.Empty;
    private byte[] PublicKey { get; set; } = [];
    private byte[] VerifyToken { get; set; } = [];
    private bool ShouldAuthenticate { get; set; }
    public EnumProtocolVersion ClientProtocolVersion { get; set; }

    public void ReadFromBuffer(IByteBuffer buffer)
    {
        if (ClientProtocolVersion == EnumProtocolVersion.V1076)
        {
            ServerId = buffer.ReadStringFromBuffer(MaxServerIdLength);
            PublicKey = buffer.ReadByteArrayFromBuffer(buffer.ReadShort());
            VerifyToken = buffer.ReadByteArrayFromBuffer(buffer.ReadShort());
            return;
        }

        ServerId = buffer.ReadStringFromBuffer(MaxServerIdLength);
        PublicKey = buffer.ReadByteArrayFromBuffer();
        VerifyToken = buffer.ReadByteArrayFromBuffer();

        if (ClientProtocolVersion is EnumProtocolVersion.V1206 or EnumProtocolVersion.V1210)
        {
            ShouldAuthenticate = buffer.ReadBoolean();
        }
    }

    public void WriteToBuffer(IByteBuffer buffer)
    {
        if (ClientProtocolVersion == EnumProtocolVersion.V1076)
        {
            buffer.WriteStringToBuffer(ServerId)
                  .WriteShort(PublicKey.Length)
                  .WriteBytes(PublicKey)
                  .WriteShort(VerifyToken.Length)
                  .WriteBytes(VerifyToken);
            return;
        }

        buffer.WriteStringToBuffer(ServerId)
              .WriteByteArrayToBuffer(PublicKey)
              .WriteByteArrayToBuffer(VerifyToken);

        if (ClientProtocolVersion == EnumProtocolVersion.V1210)
        {
            buffer.WriteBoolean(ShouldAuthenticate);
        }
    }

    public bool HandlePacket(GameConnection connection)
    {
        var keyGen = new CipherKeyGenerator();
        keyGen.Init(new KeyGenerationParameters(new SecureRandom(), KeySize));
        var publicKeyInfo = SubjectPublicKeyInfo.GetInstance(PublicKey);
        var secretKey = keyGen.GenerateKey();

        var serverHash = ComputeServerHash(secretKey);

        var encryptEvent = new EventEncryptionRequest(connection, serverHash);
        if (!EventManager.Instance.TriggerEvent("channel_interceptor", encryptEvent).IsCancelled)
        {
            connection.OnJoinServer?.Invoke(serverHash);
        }

        var cipher = new Pkcs1Encoding(new RsaEngine());
        cipher.Init(true, PublicKeyFactory.CreateKey(publicKeyInfo));

        var response = new CPacketEncryptionResponse
        {
            ClientProtocolVersion = ClientProtocolVersion,
            SecretKeyEncrypted = cipher.ProcessBlock(secretKey, 0, secretKey.Length),
            VerifyTokenEncrypted = cipher.ProcessBlock(VerifyToken, 0, VerifyToken.Length)
        };

        if (connection.ServerChannel == null)
        {
            Log.Error("Server channel is null");
            return false;
        }

        connection.ServerChannel.Configuration.AutoRead = false;
        connection.ServerChannel.Configuration.SetOption(ChannelOption.AutoRead, false);

        connection.ServerChannel.WriteAndFlushAsync(response).ContinueWith(task =>
        {
            if (!task.IsCompletedSuccessfully) return;

            try
            {
                Log.Debug("Successfully sent encryption response to client");
                GameConnection.EnableEncryption(connection.ServerChannel, secretKey);
                connection.ServerChannel.Configuration.AutoRead = true;
                connection.ServerChannel.Configuration.SetOption(ChannelOption.AutoRead, true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to enable encryption");
            }
        });

        return true;
    }

    private string ComputeServerHash(byte[] secretKey)
    {
        using var stream = new MemoryStream(MaxServerIdLength);
        stream.Write(Encoding.GetEncoding("ISO-8859-1").GetBytes(ServerId));
        stream.Write(secretKey);
        stream.Write(PublicKey);
        stream.Position = 0;
        return stream.ToSha1();
    }
}
