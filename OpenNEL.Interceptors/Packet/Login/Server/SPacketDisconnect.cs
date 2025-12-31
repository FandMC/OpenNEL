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

namespace OpenNEL.Interceptors.Packet.Login.Server;

[RegisterPacket(EnumConnectionState.Login, EnumPacketDirection.ClientBound, 0, false)]
public sealed class SPacketDisconnect : IPacket
{
    private const int MaxReasonLength = 32767;

    public string Reason { get; set; } = string.Empty;
    public EnumProtocolVersion ClientProtocolVersion { get; set; }

    public void ReadFromBuffer(IByteBuffer buffer)
    {
        Reason = buffer.ReadStringFromBuffer(MaxReasonLength);
    }

    public void WriteToBuffer(IByteBuffer buffer)
    {
        buffer.WriteStringToBuffer(Reason);
    }

    public bool HandlePacket(GameConnection connection)
    {
        Log.Debug("Disconnect reason: {Reason}", Reason);
        return false;
    }
}
