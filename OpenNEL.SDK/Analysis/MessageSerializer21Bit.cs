using OpenNEL.SDK.Extensions;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;

namespace OpenNEL.SDK.Analysis;

public class MessageSerializer21Bit : MessageToByteEncoder<IByteBuffer>
{
	protected override void Encode(IChannelHandlerContext context, IByteBuffer message, IByteBuffer output)
	{
		int readableBytes = message.ReadableBytes;
		int varIntSize = readableBytes.GetVarIntSize();
		if (varIntSize <= 3)
		{
			output.EnsureWritable(varIntSize + readableBytes);
			output.WriteVarInt(readableBytes);
			output.WriteBytes(message, message.ReaderIndex, readableBytes);
		}
	}
}
