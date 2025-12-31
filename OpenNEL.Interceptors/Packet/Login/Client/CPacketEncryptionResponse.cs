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

using DotNetty.Buffers;
using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Enums;
using OpenNEL.SDK.Extensions;
using OpenNEL.SDK.Packet;

namespace OpenNEL.Interceptors.Packet.Login.Client;

[RegisterPacket(
    EnumConnectionState.Login,
    EnumPacketDirection.ServerBound,
    1,
    [EnumProtocolVersion.V1076, EnumProtocolVersion.V108X, EnumProtocolVersion.V1200,
     EnumProtocolVersion.V1122, EnumProtocolVersion.V1180, EnumProtocolVersion.V1210,
     EnumProtocolVersion.V1206],
    false)]
public sealed class CPacketEncryptionResponse : IPacket
{
    public required byte[] SecretKeyEncrypted { get; set; } = [];
    public required byte[] VerifyTokenEncrypted { get; set; } = [];
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
            buffer.WriteShort(SecretKeyEncrypted.Length)
                  .WriteBytes(SecretKeyEncrypted)
                  .WriteShort(VerifyTokenEncrypted.Length)
                  .WriteBytes(VerifyTokenEncrypted);
        }
        else
        {
            buffer.WriteByteArrayToBuffer(SecretKeyEncrypted)
                  .WriteByteArrayToBuffer(VerifyTokenEncrypted);
        }
    }

    public bool HandlePacket(GameConnection connection) => false;
}
