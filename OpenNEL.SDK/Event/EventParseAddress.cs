using OpenNEL.SDK.Connection;
using OpenNEL.SDK.Manager;

namespace OpenNEL.SDK.Event;

public class EventParseAddress : EventArgsBase
{
	public string Address { get; set; }

	public int Port { get; set; }

	public EventParseAddress(GameConnection connection, string address, int port)
		: base(connection)
	{
		Address = address;
		Port = port;
	}
}
