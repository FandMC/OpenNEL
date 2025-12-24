using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace OpenNEL.SDK.Utils;

public static class NetworkUtil
{
	public static int GetAvailablePort(int low = 25565, int high = 35565, bool reuseTimeWait = false)
	{
		if (low > high)
		{
			return 0;
		}
		HashSet<int> usedPorts = GetUsedPorts(reuseTimeWait);
		for (int i = low; i <= high; i++)
		{
			if (!usedPorts.Contains(i))
			{
				return i;
			}
		}
		return 0;
	}

	private static HashSet<int> GetUsedPorts(bool reuseTimeWait = true)
	{
		IPGlobalProperties iPGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
		IEnumerable<int> first = from e in iPGlobalProperties.GetActiveTcpListeners()
			select e.Port;
		IEnumerable<int> second = from e in iPGlobalProperties.GetActiveUdpListeners()
			select e.Port;
		TcpConnectionInformation[] activeTcpConnections = iPGlobalProperties.GetActiveTcpConnections();
		IEnumerable<TcpConnectionInformation> source;
		if (!reuseTimeWait)
		{
			IEnumerable<TcpConnectionInformation> enumerable = activeTcpConnections;
			source = enumerable;
		}
		else
		{
			source = activeTcpConnections.Where((TcpConnectionInformation c) => c.State != TcpState.TimeWait && c.State != TcpState.CloseWait);
		}
		IEnumerable<int> second2 = source.Select((TcpConnectionInformation c) => c.LocalEndPoint.Port);
		HashSet<int> hashSet = new HashSet<int>();
		foreach (int item in first.Concat(second).Concat(second2))
		{
			hashSet.Add(item);
		}
		return hashSet;
	}
}
