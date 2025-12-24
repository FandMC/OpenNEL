using OpenNEL.SDK.Connection;

namespace OpenNEL.SDK.Manager;

public abstract class EventArgsBase(GameConnection connection) : IEventArgs
{
	public GameConnection Connection { get; set; } = connection;

	public bool IsCancelled { get; set; }
}
