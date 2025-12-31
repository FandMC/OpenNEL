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

namespace OpenNEL.Interceptors.Packet.Login.Server;

[RegisterPacket(
    EnumConnectionState.Login,
    EnumPacketDirection.ClientBound,
    3,
    [EnumProtocolVersion.V108X, EnumProtocolVersion.V1200, EnumProtocolVersion.V1122,
     EnumProtocolVersion.V1180, EnumProtocolVersion.V1210, EnumProtocolVersion.V1206],
    false)]
public sealed class SPacketEnableCompression : IPacket
{
    private int CompressionThreshold { get; set; }
    public EnumProtocolVersion ClientProtocolVersion { get; set; }

    public void ReadFromBuffer(IByteBuffer buffer)
    {
        CompressionThreshold = buffer.ReadVarIntFromBuffer();
    }

    public void WriteToBuffer(IByteBuffer buffer)
    {
        buffer.WriteVarInt(CompressionThreshold);
    }

    public bool HandlePacket(GameConnection connection)
    {
        GameConnection.EnableCompression(connection.ServerChannel, CompressionThreshold);
        return true;
    }
}
