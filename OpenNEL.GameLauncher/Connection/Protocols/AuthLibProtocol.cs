using System.Net;
using System.Net.Sockets;
using System.Text;
using OpenNEL.SDK.Entities;
using OpenNEL.SDK.Manager;
using Serilog;

namespace OpenNEL.GameLauncher.Connection.Protocols;

public class AuthLibProtocol(IPAddress address, int port, string modList, string version, string accessToken) : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private TcpListener? _listener;
    private Task? _acceptLoopTask;
    private bool _disposed;

    ~AuthLibProtocol()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if (disposing)
        {
            _cts.Cancel();
            _listener?.Stop();
            try
            {
                _acceptLoopTask?.Wait(TimeSpan.FromSeconds(5L));
            }
            catch (Exception ex)
            {
                Log.Error("Authentication failed. {Message}", ex.Message);
            }
            _cts.Dispose();
        }
        _disposed = true;
    }

    public void Start()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AuthLibProtocol));
        }
        _listener = new TcpListener(address, port);
        _listener.Start();
        _acceptLoopTask = AcceptLoopAsync(_cts.Token);
    }

    public void Stop()
    {
        if (!_disposed)
        {
            Dispose();
        }
    }

    private async Task AcceptLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && !_disposed)
        {
            try
            {
                var client = await _listener!.AcceptTcpClientAsync(token).ConfigureAwait(false);
                _ = HandleClientAsync(client, token);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Warning("Accept loop error: {Message}", ex.Message);
                break;
            }
        }
    }

    private static async Task ReadExactAsync(NetworkStream stream, byte[] buffer, int offset, int count, CancellationToken token)
    {
        int read = 0;
        while (read < count)
        {
            int bytesRead = await stream.ReadAsync(buffer.AsMemory(offset + read, count - read), token).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                throw new EndOfStreamException();
            }
            read += bytesRead;
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        using (client)
        {
            await using NetworkStream stream = client.GetStream();
            uint responseCode = 1u;
            try
            {
                byte[] lenBuf = new byte[4];
                
                // Read gameId
                await ReadExactAsync(stream, lenBuf, 0, 4, token).ConfigureAwait(false);
                int gameIdLen = BitConverter.ToInt32(lenBuf, 0);
                byte[] gameIdBuf = new byte[gameIdLen];
                await ReadExactAsync(stream, gameIdBuf, 0, gameIdLen, token).ConfigureAwait(false);
                string gameId = Encoding.UTF8.GetString(gameIdBuf);
                
                // Read userId
                await ReadExactAsync(stream, lenBuf, 0, 4, token).ConfigureAwait(false);
                int userIdLen = BitConverter.ToInt32(lenBuf, 0);
                byte[] userIdBuf = new byte[userIdLen];
                await ReadExactAsync(stream, userIdBuf, 0, userIdLen, token).ConfigureAwait(false);
                string userId = Encoding.UTF8.GetString(userIdBuf);
                
                // Read cert
                await ReadExactAsync(stream, lenBuf, 0, 4, token).ConfigureAwait(false);
                int certLen = BitConverter.ToInt32(lenBuf, 0);
                byte[] certBuf = new byte[certLen];
                await ReadExactAsync(stream, certBuf, 0, certLen, token).ConfigureAwait(false);
                string serverId = Encoding.Unicode.GetString(certBuf);
                
                EntityAvailableUser? entityAvailableUser = IUserManager.Instance?.GetAvailableUser(userId);
                if (entityAvailableUser == null)
                {
                    throw new Exception("User not found");
                }
                
                if (!string.IsNullOrEmpty(serverId))
                {
                    await NetEaseConnection.CreateAuthenticatorAsync(
                        serverId, 
                        gameId, 
                        version, 
                        modList, 
                        accessToken, 
                        int.Parse(userId), 
                        entityAvailableUser.AccessToken, 
                        "45.253.165.190", 
                        NetEaseConnection.RandomAuthPort(), 
                        () => { responseCode = 0u; }
                    ).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Client handling error: {Message}", ex.Message);
            }
            finally
            {
                try
                {
                    byte[] bytes = BitConverter.GetBytes(responseCode);
                    await stream.WriteAsync(bytes, token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Warning("Response writing error: {Message}", ex.Message);
                }
            }
        }
    }
}
