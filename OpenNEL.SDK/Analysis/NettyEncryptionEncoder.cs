using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace OpenNEL.SDK.Analysis;

public class NettyEncryptionEncoder : MessageToByteEncoder<IByteBuffer>
{
	private readonly CfbBlockCipher _encryptor;

	public NettyEncryptionEncoder(byte[] key)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Expected O, but got Unknown
		_encryptor = new CfbBlockCipher((IBlockCipher)new AesEngine(), 8);
		_encryptor.Init(true, (ICipherParameters)new ParametersWithIV((ICipherParameters)new KeyParameter(key), key));
	}

	protected override void Encode(IChannelHandlerContext context, IByteBuffer message, IByteBuffer output)
	{
		int readableBytes = message.ReadableBytes;
		output.EnsureWritable(readableBytes);
		int num = readableBytes + message.ArrayOffset + message.ReaderIndex;
		int num2 = output.ArrayOffset;
		output.SetWriterIndex(readableBytes);
		for (int i = message.ArrayOffset + message.ReaderIndex; i < num; i++)
		{
			_encryptor.ProcessBlock(message.Array, i, output.Array, num2);
			num2++;
		}
		message.SkipBytes(readableBytes);
	}
}
