using OpenNEL.SDK.Extensions;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace OpenNEL.SDK.Analysis;

public class NettyCompressionEncoder : MessageToByteEncoder<IByteBuffer>
{
	private readonly byte[] _buffer = new byte[4096];

	private readonly Deflater _deflater = new Deflater();

	public int Threshold { get; set; }

	public NettyCompressionEncoder(int threshold)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		Threshold = threshold;
	}

	protected override void Encode(IChannelHandlerContext context, IByteBuffer message, IByteBuffer output)
	{
		int readableBytes = message.ReadableBytes;
		if (readableBytes < Threshold)
		{
			output.WriteVarInt(0);
			output.WriteBytes(message);
			return;
		}
		_deflater.Reset();
		_deflater.SetInput(message.Array, message.ArrayOffset + message.ReaderIndex, message.ReadableBytes);
		message.SetReaderIndex(message.ReaderIndex + message.ReadableBytes);
		_deflater.Finish();
		output.WriteVarInt(readableBytes);
		while (!_deflater.IsFinished)
		{
			output.WriteBytes(_buffer, 0, _deflater.Deflate(_buffer));
		}
	}
}
