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
using OpenNEL.SDK.Packet;

namespace OpenNEL.Interceptors.Packet.Configuration.Server;

[RegisterPacket(
    EnumConnectionState.Configuration,
    EnumPacketDirection.ClientBound,
    3,
    [EnumProtocolVersion.V1206, EnumProtocolVersion.V1210],
    false)]
public sealed class SFinishConfiguration : IPacket
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
        return false;
    }
}
