using System;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Common;

namespace Codexus.Base1200.Plugin.Utils.Nbt;

public static class TextComponentSerializer
{
	public static IByteBuffer Serialize(TextComponent component, IByteBufferAllocator? allocator = null)
	{
		if (allocator == null)
		{
			allocator = (IByteBufferAllocator?)(object)PooledByteBufferAllocator.Default;
		}
		IByteBuffer val = allocator.Buffer();
		try
		{
			val.WriteByte(10);
			val.WriteByte(8);
			WriteString(val, "text");
			WriteString(val, component.Text);
			if (!string.IsNullOrEmpty(component.Color))
			{
				val.WriteByte(8);
				WriteString(val, "color");
				WriteString(val, component.Color);
			}
			val.WriteByte(0);
			return val;
		}
		catch
		{
			((IReferenceCounted)val).Release();
			throw;
		}
	}

	public static TextComponent Deserialize(IByteBuffer buffer)
	{
		SkipNbtCompound(buffer);
		return new TextComponent();
	}

	private static void SkipNbtCompound(IByteBuffer buffer)
	{
		var tagType = buffer.ReadByte();
		if (tagType != 10) return;
		while (true)
		{
			var type = buffer.ReadByte();
			if (type == 0) break;
			SkipString(buffer);
			SkipNbtValue(buffer, type);
		}
	}

	private static void SkipNbtValue(IByteBuffer buffer, byte type)
	{
		switch (type)
		{
			case 1: buffer.SkipBytes(1); break;
			case 2: buffer.SkipBytes(2); break;
			case 3: buffer.SkipBytes(4); break;
			case 4: buffer.SkipBytes(8); break;
			case 5: buffer.SkipBytes(4); break;
			case 6: buffer.SkipBytes(8); break;
			case 7: buffer.SkipBytes(buffer.ReadInt()); break;
			case 8: SkipString(buffer); break;
			case 9:
				var listType = buffer.ReadByte();
				var listLen = buffer.ReadInt();
				for (int i = 0; i < listLen; i++) SkipNbtValue(buffer, listType);
				break;
			case 10:
				while (true)
				{
					var t = buffer.ReadByte();
					if (t == 0) break;
					SkipString(buffer);
					SkipNbtValue(buffer, t);
				}
				break;
			case 11: buffer.SkipBytes(buffer.ReadInt() * 4); break;
			case 12: buffer.SkipBytes(buffer.ReadInt() * 8); break;
		}
	}

	private static void SkipString(IByteBuffer buffer)
	{
		var len = buffer.ReadUnsignedShort();
		buffer.SkipBytes(len);
	}

	private static void WriteString(IByteBuffer buffer, string value)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(value);
		buffer.WriteUnsignedShort((ushort)bytes.Length);
		buffer.WriteBytes(bytes);
	}
}
