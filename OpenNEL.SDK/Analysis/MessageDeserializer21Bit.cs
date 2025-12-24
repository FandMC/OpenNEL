using System;
using System.Collections.Generic;
using OpenNEL.SDK.Extensions;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using Serilog;

namespace OpenNEL.SDK.Analysis;

public class MessageDeserializer21Bit : ByteToMessageDecoder
{
	protected override void Decode(IChannelHandlerContext context, IByteBuffer message, List<object> output)
	{
		message.MarkReaderIndex();
		byte[] array = new byte[3];
		for (int i = 0; i < 3; i++)
		{
			if (!message.IsReadable())
			{
				message.ResetReaderIndex();
				break;
			}
			array[i] = message.ReadByte();
			if (array[i] >= 128)
			{
				continue;
			}
			try
			{
				int num = array.ReadVarInt();
				if (message.ReadableBytes >= num)
				{
					output.Add(message.ReadBytes(num));
				}
				else
				{
					message.ResetReaderIndex();
				}
				break;
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to decode message.", Array.Empty<object>());
				break;
			}
		}
	}
}
