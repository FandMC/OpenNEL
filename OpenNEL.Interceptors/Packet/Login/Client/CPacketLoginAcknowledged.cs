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
using OpenNEL.SDK.Event;
using OpenNEL.SDK.Manager;
using OpenNEL.SDK.Packet;
using OpenNEL.SDK.Utils;
using Serilog;

namespace OpenNEL.Interceptors.Packet.Login.Client;

[RegisterPacket(
    EnumConnectionState.Login,
    EnumPacketDirection.ServerBound,
    3,
    [EnumProtocolVersion.V1210, EnumProtocolVersion.V1206],
    false)]
public sealed class CPacketLoginAcknowledged : IPacket
{
    public EnumProtocolVersion ClientProtocolVersion { get; set; }

    public void ReadFromBuffer(IByteBuffer buffer) { }

    public void WriteToBuffer(IByteBuffer buffer) { }

    public bool HandlePacket(GameConnection connection)
    {
        if (EventManager.Instance.TriggerEvent(MessageChannels.AllVersions, new EventLoginSuccess(connection)).IsCancelled)
            return true;

        connection.State = EnumConnectionState.Configuration;
        Log.Debug("Configuration started.");
        return false;
    }
}
