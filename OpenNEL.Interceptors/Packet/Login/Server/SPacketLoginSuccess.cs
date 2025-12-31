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
using System.Collections.Generic;
using DotNetty.Buffers;
using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Entities;
using OpenNEL.SDK.Enums;
using OpenNEL.SDK.Event;
using OpenNEL.SDK.Extensions;
using OpenNEL.SDK.Manager;
using OpenNEL.SDK.Packet;
using OpenNEL.SDK.Utils;
using Serilog;

namespace OpenNEL.Interceptors.Packet.Login.Server;

[RegisterPacket(
    EnumConnectionState.Login,
    EnumPacketDirection.ClientBound,
    2,
    [EnumProtocolVersion.V1076, EnumProtocolVersion.V108X, EnumProtocolVersion.V1200,
     EnumProtocolVersion.V1122, EnumProtocolVersion.V1180, EnumProtocolVersion.V1210,
     EnumProtocolVersion.V1206],
    false)]
public sealed class SPacketLoginSuccess : IPacket
{
    private const int MaxUsernameLength = 16;
    private const int UuidStringLength = 36;

    private string UuidString { get; set; } = string.Empty;
    private byte[] UuidBytes { get; init; } = new byte[16];
    private string Username { get; set; } = string.Empty;
    private List<Property>? Properties { get; set; }
    private bool StrictErrorHandling { get; set; }
    public EnumProtocolVersion ClientProtocolVersion { get; set; }

    public void ReadFromBuffer(IByteBuffer buffer)
    {
        switch (ClientProtocolVersion)
        {
            case EnumProtocolVersion.V1206:
            case EnumProtocolVersion.V1210:
                buffer.ReadBytes(UuidBytes);
                Username = buffer.ReadStringFromBuffer(MaxUsernameLength);
                Properties = buffer.ReadProperties();
                StrictErrorHandling = buffer.ReadBoolean();
                break;

            case EnumProtocolVersion.V1200:
                buffer.ReadBytes(UuidBytes);
                Username = buffer.ReadStringFromBuffer(MaxUsernameLength);
                Properties = buffer.ReadProperties();
                break;

            case EnumProtocolVersion.V1076:
            case EnumProtocolVersion.V108X:
            case EnumProtocolVersion.V1122:
            case EnumProtocolVersion.V1180:
                UuidString = buffer.ReadStringFromBuffer(UuidStringLength);
                Username = buffer.ReadStringFromBuffer(MaxUsernameLength);
                break;
        }
    }

    public void WriteToBuffer(IByteBuffer buffer)
    {
        switch (ClientProtocolVersion)
        {
            case EnumProtocolVersion.V1206:
            case EnumProtocolVersion.V1210:
                buffer.WriteBytes(UuidBytes);
                buffer.WriteStringToBuffer(Username, MaxUsernameLength);
                buffer.WriteProperties(Properties);
                buffer.WriteBoolean(StrictErrorHandling);
                break;

            case EnumProtocolVersion.V1200:
                buffer.WriteBytes(UuidBytes);
                buffer.WriteStringToBuffer(Username, MaxUsernameLength);
                buffer.WriteProperties(Properties);
                break;

            case EnumProtocolVersion.V1076:
            case EnumProtocolVersion.V108X:
            case EnumProtocolVersion.V1122:
            case EnumProtocolVersion.V1180:
                buffer.WriteStringToBuffer(UuidString, UuidStringLength);
                buffer.WriteStringToBuffer(Username, MaxUsernameLength);
                break;
        }
    }

    public bool HandlePacket(GameConnection connection)
    {
        LogPlayerJoin();
        connection.Uuid = UuidBytes;

        if (ClientProtocolVersion >= EnumProtocolVersion.V1206)
            return false;

        if (EventManager.Instance.TriggerEvent(MessageChannels.AllVersions, new EventLoginSuccess(connection)).IsCancelled)
            return true;

        connection.State = EnumConnectionState.Play;
        return false;
    }

    private void LogPlayerJoin()
    {
        switch (ClientProtocolVersion)
        {
            case EnumProtocolVersion.V1200:
            case EnumProtocolVersion.V1206:
            case EnumProtocolVersion.V1210:
                Log.Information("{0}({1}) 加入了服务器", Username, new Guid(UuidBytes, bigEndian: true));
                break;

            case EnumProtocolVersion.V1076:
            case EnumProtocolVersion.V108X:
            case EnumProtocolVersion.V1122:
            case EnumProtocolVersion.V1180:
                Log.Information("{0}({1}) 加入了服务器", Username, UuidString);
                break;
        }
    }
}
