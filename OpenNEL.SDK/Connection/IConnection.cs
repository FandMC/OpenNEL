using DotNetty.Buffers;

namespace OpenNEL.SDK.Connection;

public interface IConnection
{
	void Prepare();

	void OnServerReceived(IByteBuffer buffer);

	void OnClientReceived(IByteBuffer buffer);

	void Shutdown();
}
