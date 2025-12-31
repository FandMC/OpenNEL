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
using System.Text;
using DotNetty.Buffers;
using OpenNEL.Interceptors.Event;
using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Enums;
using OpenNEL.SDK.Extensions;
using OpenNEL.SDK.Manager;
using OpenNEL.SDK.Packet;
using OpenNEL.SDK.Utils;
using Serilog;

namespace OpenNEL.Interceptors.Packet.Handshake.Client;

[RegisterPacket(EnumConnectionState.Handshaking, EnumPacketDirection.ServerBound, 0, false)]
public sealed class CHandshake : IPacket
{
    private const int MaxAddressLength = 255;

    public int ProtocolVersion { get; set; }

    public string ServerAddress { get; set; } = string.Empty;

    public ushort ServerPort { get; set; }

    public EnumConnectionState NextState { get; set; }

    public EnumProtocolVersion ClientProtocolVersion { get; set; }

    public void ReadFromBuffer(IByteBuffer buffer)
    {
        ProtocolVersion = buffer.ReadVarIntFromBuffer();
        ServerAddress = buffer.ReadStringFromBuffer(MaxAddressLength);
        ServerPort = buffer.ReadUnsignedShort();
        NextState = (EnumConnectionState)buffer.ReadVarIntFromBuffer();
    }

    public void WriteToBuffer(IByteBuffer buffer)
    {
        buffer.WriteVarInt(ProtocolVersion);
        buffer.WriteStringToBuffer(ServerAddress);
        buffer.WriteShort(ServerPort);
        buffer.WriteVarInt((int)NextState);
    }

    public bool HandlePacket(GameConnection connection)
    {
        var eventArgs = new EventHandshake(connection, this);
        if (EventManager.Instance.TriggerEvent(MessageChannels.AllVersions, eventArgs).IsCancelled)
        {
            return true;
        }

        connection.ProtocolVersion = (EnumProtocolVersion)ProtocolVersion;
        connection.State = NextState;

        LogOriginalAddress();
        UpdateForwardAddress(connection);
        LogNewAddress(connection);

        return false;
    }

    private void LogOriginalAddress()
    {
        var encodedAddress = Convert.ToBase64String(Encoding.UTF8.GetBytes(ServerAddress));
        Log.Debug("Original address: {ServerAddress}", encodedAddress);
    }

    private void UpdateForwardAddress(GameConnection connection)
    {
        ServerPort = (ushort)connection.ForwardPort;
        ServerAddress = BuildForwardAddress(connection);
    }

    private static string BuildForwardAddress(GameConnection connection)
    {
        var forgeMarker = connection.ProtocolVersion switch
        {
            <= EnumProtocolVersion.V1122 => "\0FML\0",
            <= EnumProtocolVersion.V1180 => "\0FML2\0",
            <= EnumProtocolVersion.V1206 => "\0FML3\0",
            _ => "\0FORGE"
        };

        return connection.ForwardAddress + forgeMarker;
    }

    private void LogNewAddress(GameConnection connection)
    {
        var encodedAddress = Convert.ToBase64String(Encoding.UTF8.GetBytes(ServerAddress));
        Log.Debug("New address: {ServerAddress}", encodedAddress);
        Log.Debug(
            "Protocol version: {ProtocolVersion}, Next state: {State}, Address: {Address}",
            connection.ProtocolVersion,
            connection.State,
            ServerAddress.Replace("\0", "|"));
    }
}
