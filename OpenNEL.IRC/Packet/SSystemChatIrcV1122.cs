using DotNetty.Buffers;
using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Enums;
using OpenNEL.SDK.Packet;
using System.Text;
using System.Text.Json;
using OpenNEL.SDK.Extensions;

namespace OpenNEL.IRC.Packet;

[RegisterPacket(EnumConnectionState.Play, EnumPacketDirection.ClientBound, 15, EnumProtocolVersion.V1122, false)]
public class SSystemChatIrcV1122 : IPacket
{
    public EnumProtocolVersion ClientProtocolVersion { get; set; }
    byte[]? _raw;
    string _json = string.Empty;
    byte _position;

    public void ReadFromBuffer(IByteBuffer buf) 
    { 
        _raw = new byte[buf.ReadableBytes]; 
        buf.GetBytes(buf.ReaderIndex, _raw); 
        _json = NettyExtensions.ReadStringFromBuffer(buf, 32767);
        _position = buf.ReadByte();
    }
    
    public void WriteToBuffer(IByteBuffer buf) 
    { 
        if (_raw != null) buf.WriteBytes(_raw);
    }

    public bool HandlePacket(GameConnection conn)
    {
        if (string.IsNullOrEmpty(_json)) return false;
        
        var players = IrcManager.GetAllOnlinePlayers();
        if (players.Count == 0) return false;

        var modifiedJson = _json;
        foreach (var kv in players)
        {
            var name = kv.Key;
            var user = kv.Value;
            var newName = $"§b[OpenNEL {user}]§r {name}";
            modifiedJson = modifiedJson.Replace($"\"{name}\"", $"\"{newName}\"");
        }
        
        if (modifiedJson != _json)
        {
            // 重新构建原始字节
            var newBytes = Encoding.UTF8.GetBytes(modifiedJson);
            var lenBytes = BitConverter.GetBytes((short)newBytes.Length);
            if (BitConverter.IsLittleEndian) Array.Reverse(lenBytes);
            
            _raw = new byte[2 + newBytes.Length + 1];  // Length + Json + Position
            Array.Copy(lenBytes, 0, _raw, 0, 2);
            Array.Copy(newBytes, 0, _raw, 2, newBytes.Length);
            _raw[^1] = _position;  // Position at end
        }
        return false;
    }
}