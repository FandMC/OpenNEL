/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using DotNetty.Buffers;
using Codexus.Development.SDK.Connection;
using Codexus.Development.SDK.Enums;
using Codexus.Development.SDK.Extensions;
using Codexus.Development.SDK.Packet;
using Serilog;

namespace OpenNEL.IRC.Packet;

[RegisterPacket(EnumConnectionState.Play, EnumPacketDirection.ServerBound, 2, EnumProtocolVersion.V1210, false)]
public class CChatCommandIrcV1210 : IPacket
{
    public EnumProtocolVersion ClientProtocolVersion { get; set; }

    private byte[]? _rawBytes;
    private string _command = string.Empty;
    private bool _isIrcCommand;

    public void ReadFromBuffer(IByteBuffer buffer)
    {
        _rawBytes = new byte[buffer.ReadableBytes];
        buffer.GetBytes(buffer.ReaderIndex, _rawBytes);

        _command = buffer.ReadStringFromBuffer(32767);
        buffer.SkipBytes(buffer.ReadableBytes);

        _isIrcCommand = _command.StartsWith("irc ", StringComparison.OrdinalIgnoreCase)
                     || _command.Equals("irc", StringComparison.OrdinalIgnoreCase);
    }

    public void WriteToBuffer(IByteBuffer buffer)
    {
        if (_isIrcCommand) return;

        if (_rawBytes != null)
            buffer.WriteBytes(_rawBytes);
    }

    public bool HandlePacket(GameConnection connection)
    {
        if (!_isIrcCommand) return false;

        var content = _command.Length > 4 ? _command.Substring(4).Trim() : string.Empty;

        if (string.IsNullOrWhiteSpace(content))
        {
            SendLocalMessage(connection, "§e[IRC] 用法: /irc <消息>");
            return true;
        }

        var playerName = connection.NickName;
        if (string.IsNullOrEmpty(playerName))
        {
            SendLocalMessage(connection, "§c[IRC] 未登录");
            return true;
        }

        var ircClient = IrcManager.Get(connection);
        if (ircClient == null)
        {
            SendLocalMessage(connection, "§c[IRC] IRC 未连接");
            return true;
        }
        ircClient.SendChat(playerName, content);
        return true;
    }

    public static void SendLocalMessage(GameConnection connection, string message)
    {
        try
        {
            if (connection.State != EnumConnectionState.Play) return;
            if (connection.ProtocolVersion != EnumProtocolVersion.V1210) return;
            var buffer = Unpooled.Buffer();
            buffer.WriteVarInt(0x6C);
            var textBytes = System.Text.Encoding.UTF8.GetBytes(message);
            buffer.WriteByte(0x08);
            buffer.WriteShort(textBytes.Length);
            buffer.WriteBytes(textBytes);
            buffer.WriteBoolean(false);
            connection.ClientChannel?.WriteAndFlushAsync(buffer);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[IRC] 发送本地消息失败");
        }
    }
}
