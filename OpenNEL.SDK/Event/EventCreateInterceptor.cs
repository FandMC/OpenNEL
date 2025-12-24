using OpenNEL.SDK.Manager;

namespace OpenNEL.SDK.Event;

public class EventCreateInterceptor(int port) : IEventArgs
{
	public int Port { get; set; } = port;

	public bool IsCancelled { get; set; }
}
