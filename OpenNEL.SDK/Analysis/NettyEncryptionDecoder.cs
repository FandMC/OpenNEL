using System.Collections.Generic;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace OpenNEL.SDK.Analysis;

public class NettyEncryptionDecoder : ByteToMessageDecoder
{
	private readonly CfbBlockCipher _decipher;

	public NettyEncryptionDecoder(byte[] key)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Expected O, but got Unknown
		_decipher = new CfbBlockCipher((IBlockCipher)new AesEngine(), 8);
		_decipher.Init(false, (ICipherParameters)new ParametersWithIV((ICipherParameters)new KeyParameter(key), key));
	}

	protected override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
	{
		int readableBytes = input.ReadableBytes;
		IByteBuffer val = context.Allocator.HeapBuffer(readableBytes);
		int num = input.ReaderIndex + input.ArrayOffset + readableBytes;
		int num2 = val.ArrayOffset;
		for (int i = input.ReaderIndex + input.ArrayOffset; i < num; i++)
		{
			_decipher.ProcessBlock(input.Array, i, val.Array, num2);
			num2++;
		}
		val.SetWriterIndex(readableBytes);
		input.SkipBytes(readableBytes);
		output.Add(val);
	}
}
