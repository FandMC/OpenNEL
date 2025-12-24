using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace OpenNEL.SDK.Utils;

public class UdpBroadcaster : IDisposable
{
	private readonly bool _is189Protocol;

	private readonly string _roleName;

	private readonly string _serverIp;

	private readonly int _serverPort;

	private readonly IPEndPoint _targetEndPoint;

	private readonly UdpClient _udpClient;

	private CancellationTokenSource? _cts;

	public UdpBroadcaster(string multicastAddress, int port, int targetPort, string serverIp, string roleName, bool is189Protocol)
	{
		_udpClient = new UdpClient();
		_udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, optionValue: true);
		_targetEndPoint = new IPEndPoint(IPAddress.Parse(multicastAddress), port);
		if (IsMulticastAddress(_targetEndPoint.Address))
		{
			_udpClient.JoinMulticastGroup(_targetEndPoint.Address);
			_udpClient.MulticastLoopback = true;
		}
		else
		{
			_udpClient.EnableBroadcast = true;
		}
		_serverPort = targetPort;
		_is189Protocol = is189Protocol;
		_serverIp = serverIp;
		_roleName = roleName;
	}

	public void Dispose()
	{
		_cts?.Dispose();
		_udpClient?.Close();
		_udpClient?.Dispose();
	}

	public async Task StartBroadcastingAsync()
	{
		_cts = new CancellationTokenSource();
		try
		{
			while (!_cts.IsCancellationRequested)
			{
				await SendMessageAsync();
				await Task.Delay(TimeSpan.FromSeconds(2L), _cts.Token);
			}
		}
		catch (OperationCanceledException ex)
		{
			OperationCanceledException ex2 = ex;
			Log.Error("Broadcasting operation cancelled, {exception}", new object[1] { ex2.Message });
		}
		catch (Exception ex3)
		{
			Exception value = ex3;
			Log.Error($"UDP Broadcast error: {value}", Array.Empty<object>());
		}
	}

	private async Task SendMessageAsync()
	{
		try
		{
			string s = BuildMessage();
			byte[] bytes = Encoding.UTF8.GetBytes(s);
			await _udpClient.SendAsync(bytes, bytes.Length, _targetEndPoint);
		}
		catch (SocketException ex) when (ex.SocketErrorCode == SocketError.HostUnreachable)
		{
			await Task.Delay(5000, _cts.Token);
		}
		catch (Exception ex2)
		{
			Exception value = ex2;
			Log.Error($"UDP Send failed: {value}", Array.Empty<object>());
		}
	}

	private string BuildMessage()
	{
		if (!_is189Protocol)
		{
			return $"[MOTD] §bOpenNEL §e{_serverIp} -> §a{_roleName}[/MOTD][AD]{_serverPort}[/AD]";
		}
		return $"[MOTD] OpenNEL {_serverIp} -> {_roleName}[/MOTD][AD]{_serverPort}[/AD]";
	}

	public void Stop()
	{
		_cts?.Cancel();
	}

	private static bool IsMulticastAddress(IPAddress address)
	{
		byte[] addressBytes = address.GetAddressBytes();
		if (address.AddressFamily == AddressFamily.InterNetwork && addressBytes[0] >= 224)
		{
			return addressBytes[0] <= 239;
		}
		return false;
	}
}
