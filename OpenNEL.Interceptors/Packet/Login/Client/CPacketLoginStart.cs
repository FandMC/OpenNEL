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
using Serilog;

namespace OpenNEL.Interceptors.Packet.Login.Client;

[RegisterPacket(
    EnumConnectionState.Login,
    EnumPacketDirection.ServerBound,
    0,
    [EnumProtocolVersion.V1076, EnumProtocolVersion.V108X, EnumProtocolVersion.V1200,
     EnumProtocolVersion.V1122, EnumProtocolVersion.V1180, EnumProtocolVersion.V1210,
     EnumProtocolVersion.V1206],
    false)]
public sealed class CPacketLoginStart : IPacket
{
    private const int MaxProfileLength = 16;

    private string Profile { get; set; } = string.Empty;
    private bool HasPlayerUuid { get; set; }
    private byte[] Uuid { get; } = new byte[16];
    public EnumProtocolVersion ClientProtocolVersion { get; set; }

    public void ReadFromBuffer(IByteBuffer buffer)
    {
        switch (ClientProtocolVersion)
        {
            case EnumProtocolVersion.V1206:
            case EnumProtocolVersion.V1210:
                Profile = buffer.ReadStringFromBuffer(MaxProfileLength);
                buffer.ReadBytes(Uuid);
                break;

            case EnumProtocolVersion.V1200:
                Profile = buffer.ReadStringFromBuffer(MaxProfileLength);
                HasPlayerUuid = buffer.ReadBoolean();
                if (HasPlayerUuid)
                    buffer.ReadBytes(Uuid);
                break;

            case EnumProtocolVersion.V1076:
            case EnumProtocolVersion.V108X:
            case EnumProtocolVersion.V1122:
            case EnumProtocolVersion.V1180:
                Profile = buffer.ReadStringFromBuffer(MaxProfileLength);
                break;
        }
    }

    public void WriteToBuffer(IByteBuffer buffer)
    {
        switch (ClientProtocolVersion)
        {
            case EnumProtocolVersion.V1206:
            case EnumProtocolVersion.V1210:
                buffer.WriteStringToBuffer(Profile, MaxProfileLength);
                buffer.WriteBytes(Uuid);
                break;

            case EnumProtocolVersion.V1200:
                buffer.WriteStringToBuffer(Profile, MaxProfileLength);
                buffer.WriteBoolean(HasPlayerUuid);
                if (HasPlayerUuid)
                    buffer.WriteBytes(Uuid);
                break;

            case EnumProtocolVersion.V1180:
                buffer.WriteStringToBuffer(Profile);
                break;

            case EnumProtocolVersion.V1076:
            case EnumProtocolVersion.V108X:
            case EnumProtocolVersion.V1122:
                buffer.WriteStringToBuffer(Profile, MaxProfileLength);
                break;
        }
    }

    public bool HandlePacket(GameConnection connection)
    {
        Profile = connection.NickName;
        Log.Debug("{NickName} trying to login...", Profile);
        return false;
    }
}
