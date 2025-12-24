using OpenNEL.SDK.Connection;
using DotNetty.Common.Utilities;

namespace OpenNEL.SDK.Utils;

public static class ChannelAttribute
{
	public static readonly AttributeKey<GameConnection> Connection = AttributeKey<GameConnection>.ValueOf("Connection");
}
