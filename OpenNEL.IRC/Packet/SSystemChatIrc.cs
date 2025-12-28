using DotNetty.Buffers;
using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Enums;
using OpenNEL.SDK.Packet;
using System.Text;

namespace OpenNEL.IRC.Packet;

[RegisterPacket(EnumConnectionState.Play, EnumPacketDirection.ClientBound, 108, EnumProtocolVersion.V1206, false)]
public class SSystemChatIrc : IPacket
{
    public EnumProtocolVersion ClientProtocolVersion { get; set; }
    byte[]? _raw;

    public void ReadFromBuffer(IByteBuffer buf) { _raw = new byte[buf.ReadableBytes]; buf.ReadBytes(_raw); }
    public void WriteToBuffer(IByteBuffer buf) { if (_raw != null) buf.WriteBytes(_raw); }

    public bool HandlePacket(GameConnection conn)
    {
        if (_raw == null || _raw.Length < 5) return false;
        
        var players = IrcManager.GetAllOnlinePlayers();
        if (players.Count == 0) return false;

        foreach (var kv in players)
        {
            var name = kv.Key;
            var user = kv.Value;
            var nameBytes = Encoding.UTF8.GetBytes(name);
            var newBytes = Encoding.UTF8.GetBytes($"§b[OpenNEL {user}]§r {name}");
            var idx = FindString(_raw, nameBytes);
            if (idx >= 0) _raw = ReplaceString(_raw, idx, nameBytes.Length, newBytes);
        }
        return false;
    }

    static int FindString(byte[] buf, byte[] target)
    {
        int len = target.Length;
        byte hi = (byte)(len >> 8), lo = (byte)(len & 0xFF);
        for (int i = 0; i < buf.Length - len - 2; i++)
        {
            if (buf[i] == hi && buf[i + 1] == lo)
            {
                bool ok = true;
                for (int j = 0; j < len && ok; j++) if (buf[i + 2 + j] != target[j]) ok = false;
                if (ok) return i;
            }
        }
        return -1;
    }

    static byte[] ReplaceString(byte[] buf, int idx, int oldLen, byte[] newBytes)
    {
        int newLen = newBytes.Length;
        var result = new byte[buf.Length - (2 + oldLen) + (2 + newLen)];
        Array.Copy(buf, 0, result, 0, idx);
        result[idx] = (byte)(newLen >> 8);
        result[idx + 1] = (byte)(newLen & 0xFF);
        Array.Copy(newBytes, 0, result, idx + 2, newLen);
        Array.Copy(buf, idx + 2 + oldLen, result, idx + 2 + newLen, buf.Length - idx - 2 - oldLen);
        return result;
    }
}
