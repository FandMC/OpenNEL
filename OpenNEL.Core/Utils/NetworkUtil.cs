using System.Net;
using System.Net.Sockets;

namespace OpenNEL.Core.Utils;

public static class NetworkUtil
{
    private static readonly Random Random = new();

    public static int GetAvailablePort(int startPort = 10000)
    {
        int port = startPort;
        while (port < 65535)
        {
            if (IsPortAvailable(port))
            {
                return port;
            }
            port++;
        }
        return -1;
    }

    public static int GetRandomAvailablePort(int minPort = 10000, int maxPort = 65535)
    {
        int attempts = 100;
        while (attempts > 0)
        {
            int port = Random.Next(minPort, maxPort);
            if (IsPortAvailable(port))
            {
                return port;
            }
            attempts--;
        }
        return GetAvailablePort(minPort);
    }

    public static bool IsPortAvailable(int port)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }
}
