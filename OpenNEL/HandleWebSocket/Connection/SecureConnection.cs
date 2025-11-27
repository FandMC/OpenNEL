using System;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using OpenNEL.Entities;
using OpenNEL.Extensions;
using Serilog;

namespace OpenNEL.HandleWebSocket.Connection;

public class SecureConnection
{
    private readonly ECDiffieHellman _ecDiffieHellman;

    private readonly WebSocket _client;

	public bool IsHandshaking;

	public ConnectionState State;

	public readonly AsyncLocal<string?> CurrentIdentify;

	public byte[]? SessionKey { get; set; }

	public bool IsEncrypted { get; set; }

	public Guid ClientId { get; }

	public Action<byte[]>? AddSendQueue { get; set; }

    public SecureConnection(Guid clientId, WebSocket client)
    {
        _client = client;
        _ecDiffieHellman = ECDiffieHellman.Create(EcCurveExtensions.DefaultCurve);
        ClientId = clientId;
        CurrentIdentify = new AsyncLocal<string>();
    }

	public void HandleHandshaking(ECDiffieHellmanPublicKey clientPublicKey)
	{
		IsHandshaking = true;
		byte[] ikm = _ecDiffieHellman.DeriveRawSecretAgreement(clientPublicKey);
		SessionKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, 32, "codexus.today.websocket.establishing"u8.ToArray(), "codexus.today.aes.key"u8.ToArray());
		byte[] inArray = _ecDiffieHellman.PublicKey.ExportSubjectPublicKeyInfo();
		Entity entity = new Entity("handshake", Convert.ToBase64String(inArray));
		_ecDiffieHellman.Clear();
		SendToClientAsync(entity);
		IsHandshaking = false;
		IsEncrypted = true;
	}

	public void SendToClientAsync(Entity? entity)
	{
		entity?.PrepareSignatureAndIdentify(CurrentIdentify.Value);
		SendToClientAsync(JsonSerializer.Serialize(entity));
	}

	public void SendToClientAsync(string message)
	{
		SendToClientAsync(Encoding.UTF8.GetBytes(message));
	}

	public void SendToClientAsync(byte[] message)
	{
		Log.Debug("Sending message to ui: {Message}", message);
		byte[] obj = (IsEncrypted ? message.Encrypt(SessionKey) : message);
        if (_client.State != WebSocketState.Open)
        {
            Log.Warning("Client: {ClientId} has not been established", ClientId);
            return;
        }
		try
		{
			AddSendQueue?.Invoke(obj);
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Error sending message to client, clientId: {ClientId}", ClientId);
		}
	}

	public void CloseAsync(string closeReason)
	{
        _client.CloseAsync(WebSocketCloseStatus.NormalClosure, closeReason, CancellationToken.None);
    }
}
