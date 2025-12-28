using DotNetty.Buffers;
using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Enums;
using OpenNEL.SDK.Extensions;
using OpenNEL.SDK.Packet;
using Serilog;

namespace OpenNEL.IRC.Packet;

[RegisterPacket(EnumConnectionState.Play, EnumPacketDirection.ServerBound, 0x02, EnumProtocolVersion.V1122, false)]
public class CChatMessageIrc : IPacket
{
    public EnumProtocolVersion ClientProtocolVersion { get; set; }

    byte[]? _raw;
    string _msg = string.Empty;
    bool _isIrc;

    public void ReadFromBuffer(IByteBuffer buf)
    {
        _raw = new byte[buf.ReadableBytes];
        buf.GetBytes(buf.ReaderIndex, _raw);
        _msg = NettyExtensions.ReadStringFromBuffer(buf, 256);  // 1.12.2 限制 256 字符
        buf.SkipBytes(buf.ReadableBytes);
        _isIrc = _msg.StartsWith("/irc ", StringComparison.OrdinalIgnoreCase) || _msg.Equals("/irc", StringComparison.OrdinalIgnoreCase);
        if (_isIrc) Log.Information("[IRC] 拦截到命令: {Msg}", _msg);
    }

    public void WriteToBuffer(IByteBuffer buf)
    {
        if (!_isIrc && _raw != null) buf.WriteBytes(_raw);
    }

    public bool HandlePacket(GameConnection conn)
    {
        if (!_isIrc) return false;

        var msg = _msg.Length > 5 ? _msg[5..].Trim() : "";
        if (string.IsNullOrWhiteSpace(msg)) { SendMsg(conn, "§e[IRC] /irc <消息>"); return true; }
        if (string.IsNullOrEmpty(conn.NickName)) { SendMsg(conn, "§c[IRC] 未登录"); return true; }
        
        var client = IrcManager.Get(conn);
        if (client == null) { SendMsg(conn, "§c[IRC] 未连接"); return true; }
        
        client.SendChat(conn.NickName, msg);
        return true;
    }

    static void SendMsg(GameConnection conn, string msg)
    {
        try
        {
            var buf = Unpooled.Buffer();
            buf.WriteVarInt(108);
            var bytes = System.Text.Encoding.UTF8.GetBytes(msg);
            buf.WriteByte(0x08);
            buf.WriteShort(bytes.Length);
            buf.WriteBytes(bytes);
            buf.WriteBoolean(false);
            conn.ClientChannel?.WriteAndFlushAsync(buf);
        }
        catch (Exception ex) { Log.Error(ex, "[IRC] 发送失败"); }
    }
}