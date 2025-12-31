using System;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Common;

namespace OpenNEL.Core.Utils;

public static class TextComponentSerializer
{
	public static IByteBuffer Serialize(TextComponent component, IByteBufferAllocator? allocator = null)
	{
		allocator ??= PooledByteBufferAllocator.Default;
		IByteBuffer buffer = allocator.Buffer();
		try
		{
			buffer.WriteByte(10);
			SerializeCompound(buffer, component);
			return buffer;
		}
		catch
		{
			buffer.Release();
			throw;
		}
	}

	private static void SerializeCompound(IByteBuffer buffer, TextComponent component)
	{
		bool hasTextOrTranslate = !string.IsNullOrEmpty(component.Text) || !string.IsNullOrEmpty(component.Translate);
		
		if (!string.IsNullOrEmpty(component.Text))
		{
			buffer.WriteByte(8);
			WriteString(buffer, "text");
			WriteString(buffer, component.Text);
		}
		else if (!hasTextOrTranslate)
		{
			buffer.WriteByte(8);
			WriteString(buffer, "text");
			WriteString(buffer, "");
		}
		
		if (!string.IsNullOrEmpty(component.Translate))
		{
			buffer.WriteByte(8);
			WriteString(buffer, "translate");
			WriteString(buffer, component.Translate);
		}
		
		if (!string.IsNullOrEmpty(component.Color))
		{
			buffer.WriteByte(8);
			WriteString(buffer, "color");
			WriteString(buffer, component.Color);
		}
		
		if (component.Bold)
		{
			buffer.WriteByte(1);
			WriteString(buffer, "bold");
			buffer.WriteByte(1);
		}
		
		if (component.Extra.Count > 0)
		{
			buffer.WriteByte(9);
			WriteString(buffer, "extra");
			buffer.WriteByte(10);
			buffer.WriteInt(component.Extra.Count);
			foreach (var child in component.Extra)
			{
				SerializeCompound(buffer, child);
			}
		}
		
		buffer.WriteByte(0);
	}

	public static TextComponent Deserialize(IByteBuffer buffer)
	{
		var component = new TextComponent();
		var textBuilder = new StringBuilder();
		
		byte tagType = buffer.ReadByte();
		if (tagType != 10)
		{
			buffer.SetReaderIndex(buffer.ReaderIndex - 1);
			component.Text = ReadUtf8String(buffer);
			return component;
		}
		
		DeserializeCompound(buffer, component, textBuilder);
		component.FullText = textBuilder.ToString();
		
		return component;
	}

	private static void DeserializeCompound(IByteBuffer buffer, TextComponent component, StringBuilder textBuilder)
	{
		while (true)
		{
			byte fieldType = buffer.ReadByte();
			if (fieldType == 0) break;
			
			string fieldName = ReadString(buffer);
			
			switch (fieldType)
			{
				case 1:
					byte bval = buffer.ReadByte();
					if (fieldName == "bold")
						component.Bold = bval != 0;
					break;
				case 8:
					string value = ReadString(buffer);
					if (fieldName == "text")
					{
						if (string.IsNullOrEmpty(component.Text))
							component.Text = value;
						textBuilder.Append(value);
					}
					else if (fieldName == "translate")
					{
						component.Translate = value;
						textBuilder.Append(value);
					}
					else if (fieldName == "color")
						component.Color = value;
					break;
				case 9:
					if (fieldName == "extra")
					{
						byte listType = buffer.ReadByte();
						int listLen = buffer.ReadInt();
						for (int i = 0; i < listLen; i++)
						{
							if (listType == 10)
							{
								var child = new TextComponent();
								DeserializeCompound(buffer, child, textBuilder);
								component.Extra.Add(child);
							}
							else
							{
								SkipTag(buffer, listType);
							}
						}
					}
					else
					{
						SkipList(buffer);
					}
					break;
				default:
					SkipTag(buffer, fieldType);
					break;
			}
		}
	}

	private static void SkipList(IByteBuffer buffer)
	{
		byte listType = buffer.ReadByte();
		int listLen = buffer.ReadInt();
		for (int i = 0; i < listLen; i++)
			SkipTag(buffer, listType);
	}

	private static void WriteString(IByteBuffer buffer, string value)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(value);
		buffer.WriteShort(bytes.Length);
		buffer.WriteBytes(bytes);
	}

	private static string ReadString(IByteBuffer buffer)
	{
		int length = buffer.ReadUnsignedShort();
		byte[] bytes = new byte[length];
		buffer.ReadBytes(bytes);
		return Encoding.UTF8.GetString(bytes);
	}

	private static string ReadUtf8String(IByteBuffer buffer)
	{
		int length = ReadVarInt(buffer);
		byte[] bytes = new byte[length];
		buffer.ReadBytes(bytes);
		return Encoding.UTF8.GetString(bytes);
	}

	private static int ReadVarInt(IByteBuffer buffer)
	{
		int value = 0;
		int position = 0;
		byte currentByte;
		
		while (true)
		{
			currentByte = buffer.ReadByte();
			value |= (currentByte & 0x7F) << position;
			if ((currentByte & 0x80) == 0) break;
			position += 7;
			if (position >= 32) throw new Exception("VarInt too big");
		}
		
		return value;
	}

	private static void SkipTag(IByteBuffer buffer, byte tagType)
	{
		switch (tagType)
		{
			case 1: buffer.SkipBytes(1); break;
			case 2: buffer.SkipBytes(2); break;
			case 3: buffer.SkipBytes(4); break;
			case 4: buffer.SkipBytes(8); break;
			case 5: buffer.SkipBytes(4); break;
			case 6: buffer.SkipBytes(8); break;
			case 7:
				int len7 = buffer.ReadInt();
				buffer.SkipBytes(len7);
				break;
			case 8:
				int len8 = buffer.ReadUnsignedShort();
				buffer.SkipBytes(len8);
				break;
			case 9:
				byte listType = buffer.ReadByte();
				int listLen = buffer.ReadInt();
				for (int i = 0; i < listLen; i++)
					SkipTag(buffer, listType);
				break;
			case 10:
				while (true)
				{
					byte t = buffer.ReadByte();
					if (t == 0) break;
					int nameLen = buffer.ReadUnsignedShort();
					buffer.SkipBytes(nameLen);
					SkipTag(buffer, t);
				}
				break;
			case 11:
				int len11 = buffer.ReadInt();
				buffer.SkipBytes(len11 * 4);
				break;
			case 12:
				int len12 = buffer.ReadInt();
				buffer.SkipBytes(len12 * 8);
				break;
		}
	}
}
