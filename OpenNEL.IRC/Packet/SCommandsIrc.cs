using DotNetty.Buffers;
using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Enums;
using OpenNEL.SDK.Extensions;
using OpenNEL.SDK.Packet;
using Serilog;

namespace OpenNEL.IRC.Packet;

[RegisterPacket(EnumConnectionState.Play, EnumPacketDirection.ClientBound, 17, EnumProtocolVersion.V1206, true)]
public class SCommandsIrc : IPacket
{
    public EnumProtocolVersion ClientProtocolVersion { get; set; }
    byte[]? _raw;
    List<Node> _nodes = new();
    int _rootIdx;

    public void ReadFromBuffer(IByteBuffer buf)
    {
        _raw = new byte[buf.ReadableBytes];
        buf.GetBytes(buf.ReaderIndex, _raw);
        try
        {
            int count = buf.ReadVarIntFromBuffer();
            _nodes = new List<Node>(count);
            for (int i = 0; i < count; i++) _nodes.Add(ReadNode(buf));
            _rootIdx = buf.ReadVarIntFromBuffer();
        }
        catch (Exception ex) { Log.Warning(ex, "[IRC] Commands解析失败"); _nodes.Clear(); }
    }

    public void WriteToBuffer(IByteBuffer buf)
    {
        if (_nodes.Count == 0) { if (_raw != null) buf.WriteBytes(_raw); return; }
        try
        {
            AddIrc();
            buf.WriteVarInt(_nodes.Count);
            foreach (var n in _nodes) WriteNode(buf, n);
            buf.WriteVarInt(_rootIdx);
        }
        catch { buf.Clear(); if (_raw != null) buf.WriteBytes(_raw); }
    }

    void AddIrc()
    {
        var root = _nodes[_rootIdx];
        if (root.Children.Any(i => i < _nodes.Count && _nodes[i].Name == "irc")) return;
        
        int ircIdx = _nodes.Count, msgIdx = ircIdx + 1;
        _nodes.Add(new Node { Flags = 0x05, Children = new() { msgIdx }, Name = "irc" });
        _nodes.Add(new Node { Flags = 0x06, Children = new(), Name = "message", Parser = "brigadier:string", Props = new byte[] { 2 } });
        root.Children.Add(ircIdx);
    }

    Node ReadNode(IByteBuffer buf)
    {
        var n = new Node { Flags = buf.ReadByte() };
        int cc = buf.ReadVarIntFromBuffer();
        n.Children = new(cc);
        for (int i = 0; i < cc; i++) n.Children.Add(buf.ReadVarIntFromBuffer());
        if ((n.Flags & 0x08) != 0) n.Redirect = buf.ReadVarIntFromBuffer();
        int type = n.Flags & 0x03;
        if (type is 1 or 2) n.Name = buf.ReadStringFromBuffer(32767);
        if (type == 2)
        {
            n.Parser = buf.ReadStringFromBuffer(32767);
            n.Props = ReadProps(buf, n.Parser);
            if ((n.Flags & 0x10) != 0) n.Suggest = buf.ReadStringFromBuffer(32767);
        }
        return n;
    }

    void WriteNode(IByteBuffer buf, Node n)
    {
        buf.WriteByte(n.Flags);
        buf.WriteVarInt(n.Children.Count);
        foreach (var c in n.Children) buf.WriteVarInt(c);
        if ((n.Flags & 0x08) != 0 && n.Redirect.HasValue) buf.WriteVarInt(n.Redirect.Value);
        int type = n.Flags & 0x03;
        if (type is 1 or 2) buf.WriteStringToBuffer(n.Name ?? "");
        if (type == 2)
        {
            buf.WriteStringToBuffer(n.Parser ?? "");
            if (n.Props != null) buf.WriteBytes(n.Props);
            if ((n.Flags & 0x10) != 0 && n.Suggest != null) buf.WriteStringToBuffer(n.Suggest);
        }
    }

    byte[] ReadProps(IByteBuffer buf, string p) => p switch
    {
        "brigadier:bool" => [],
        "brigadier:float" or "brigadier:double" or "brigadier:integer" or "brigadier:long" => ReadFlags(buf, 4),
        "brigadier:string" or "minecraft:entity" or "minecraft:score_holder" => new[] { buf.ReadByte() },
        "minecraft:resource" or "minecraft:resource_or_tag" or "minecraft:resource_or_tag_key" or "minecraft:resource_key" => ReadId(buf),
        _ => []
    };

    byte[] ReadFlags(IByteBuffer buf, int size)
    {
        byte f = buf.ReadByte();
        var r = new List<byte> { f };
        if ((f & 1) != 0) { var b = new byte[size]; buf.ReadBytes(b); r.AddRange(b); }
        if ((f & 2) != 0) { var b = new byte[size]; buf.ReadBytes(b); r.AddRange(b); }
        return r.ToArray();
    }

    byte[] ReadId(IByteBuffer buf)
    {
        int s = buf.ReaderIndex;
        buf.ReadStringFromBuffer(32767);
        int len = buf.ReaderIndex - s;
        buf.SetReaderIndex(s);
        var b = new byte[len];
        buf.ReadBytes(b);
        return b;
    }

    public bool HandlePacket(GameConnection conn) => false;

    class Node { public byte Flags; public List<int> Children = new(); public int? Redirect; public string? Name, Parser, Suggest; public byte[]? Props; }
}
