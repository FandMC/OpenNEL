using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OpenNEL.GameLauncher.Connection.ChaCha;
using OpenNEL.GameLauncher.Connection.Entities;
using OpenNEL.GameLauncher.Connection.Extensions;
using CoreExt = OpenNEL.Core.Extensions.ByteArrayExtensions;
using Serilog;

namespace OpenNEL.GameLauncher.Connection;

public static class NetEaseConnection
{
    private static readonly byte[] TokenKey = new byte[]
    {
        172, 36, 156, 105, 199, 44, 179, 180, 78, 192,
        204, 108, 84, 58, 129, 149
    };

    private static readonly byte[] ChaChaNonce = "163 NetEase\n"u8.ToArray();

    public static int RandomAuthPort()
    {
        int[] array = [10200, 10600, 10400, 10000];
        return array[new Random().Next(0, array.Length)];
    }

    public static async Task CreateAuthenticatorAsync(
        string serverId, 
        string gameId, 
        string gameVersion, 
        string modInfo, 
        string nexusToken, 
        int userId, 
        string userToken, 
        string authAddress, 
        int authPort, 
        Action handleSuccess, 
        Func<string, string, int, string, byte[], string, byte[]>? buildEstablishing = null, 
        Func<string, ChaChaOfSalsa, string, long, string, string, string, int, byte[], byte[]>? buildJoinServerMessage = null)
    {
        buildEstablishing ??= DefaultBuildEstablishing;
        buildJoinServerMessage ??= DefaultBuildJoinServerMessage;

        TcpClient client = new TcpClient();
        try
        {
            await client.ConnectAsync(IPAddress.Parse(authAddress), authPort);
            if (!client.Connected)
            {
                throw new TimeoutException($"Connecting to server {authAddress}:{authPort} timed out");
            }
            Log.Information("Connected to server {Address}:{Port}", authAddress, authPort);
            
            NetworkStream stream = client.GetStream();
            using MemoryStream details = await stream.ReadSteamWithInt16Async();
            byte[] context = details.ToArray();
            byte[] remoteKey = new byte[16];
            byte[] rsaKey = new byte[256];
            details.Position = 0L;
            details.ReadExactly(remoteKey);
            details.ReadExactly(rsaKey);
            
            byte[] establishMsg = buildEstablishing(nexusToken, gameVersion, userId, userToken, context, "netease");
            await stream.WriteAsync(establishMsg);
            
            using MemoryStream statusStream = await stream.ReadSteamWithInt16Async();
            byte status = (byte)statusStream.ReadByte();
            if (status != 0)
            {
                throw new Exception("Establishing error: " + Convert.ToHexString(new ReadOnlySpan<byte>(in status)));
            }
            Log.Information("Establishing successfully");
            
            byte[] tokenXored = CoreExt.Xor(Encoding.ASCII.GetBytes(userToken), TokenKey);
            ChaChaOfSalsa encryptCipher = new ChaChaOfSalsa(CoreExt.CombineWith(tokenXored, remoteKey), ChaChaNonce, encryption: true);
            ChaChaOfSalsa decryptCipher = new ChaChaOfSalsa(CoreExt.CombineWith(remoteKey, tokenXored), ChaChaNonce, encryption: false);
            
            byte[] joinMsg = buildJoinServerMessage(nexusToken, encryptCipher, serverId, long.Parse(gameId), gameVersion, modInfo, "netease", userId, remoteKey);
            await stream.WriteAsync(joinMsg);
            
            using MemoryStream responseStream = await stream.ReadSteamWithInt16Async();
            byte[] responseData = responseStream.ToArray();
            var (msgType, msgBody) = decryptCipher.UnpackMessage(responseData);
            if (msgType != 9 || msgBody[0] != 0)
            {
                throw new Exception("Authentication of message failed: " + msgBody[0]);
            }
            handleSuccess();
        }
        catch (HttpRequestException ex)
        {
            client.Close();
            if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Log.Error("Access token is invalid or expired.");
            }
        }
        catch (Exception ex2)
        {
            client.Close();
            throw new Exception("Failed to create connection: " + ex2.Message, ex2);
        }
    }

    private static byte[] DefaultBuildEstablishing(string nexusToken, string gameVersion, int userId, string userToken, byte[] context, string channel)
    {
        Log.Information("Building establishing message");
        
        // 本地实现握手逻辑
        var base64Context = Convert.ToBase64String(context);
        var handshakeData = new
        {
            userId = userId,
            userToken = userToken,
            base64Context = base64Context,
            channel = channel,
            gameVersion = gameVersion
        };
        
        // 模拟服务器返回的握手体，这里使用本地计算
        var handshakeBody = ComputeLocalHandshakeBody(handshakeData);
        
        var handshake = new EntityHandshake { HandshakeBody = handshakeBody };
        return Convert.FromBase64String(handshake?.HandshakeBody ?? string.Empty);
    }
    
    private static string ComputeLocalHandshakeBody(object handshakeData)
    {
        // 本地实现握手体计算逻辑
        // 这里应该根据实际的网易握手协议实现
        // 由于WebNexusApi被废弃，我们需要本地实现
        var userId = handshakeData.GetType().GetProperty("userId").GetValue(handshakeData);
        var userToken = handshakeData.GetType().GetProperty("userToken").GetValue(handshakeData);
        var base64Context = handshakeData.GetType().GetProperty("base64Context").GetValue(handshakeData);
        var channel = handshakeData.GetType().GetProperty("channel").GetValue(handshakeData);
        var gameVersion = handshakeData.GetType().GetProperty("gameVersion").GetValue(handshakeData);
        
        // 模拟握手体生成逻辑
        // 在实际实现中，这里应该有具体的加密/哈希算法
        var combinedData = $"{userId}:{userToken}:{base64Context}:{channel}:{gameVersion}";
        var handshakeBody = Convert.ToBase64String(Encoding.UTF8.GetBytes(combinedData));
        
        return handshakeBody;
    }

    private static byte[] DefaultBuildJoinServerMessage(string nexusToken, ChaChaOfSalsa cipher, string serverId, long gameId, string gameVersion, string modInfo, string channel, int userId, byte[] handshakeKey)
    {
        Log.Information("Building join server message");
        
        // 本地实现认证逻辑
        var authBody = ComputeLocalAuthBody(serverId, gameId, gameVersion, modInfo, channel, userId, Convert.ToBase64String(handshakeKey));
        var authDict = new Dictionary<string, string> { { "authBody", authBody } };
        return cipher.PackMessage(9, Convert.FromBase64String(authDict?["authBody"] ?? string.Empty));
    }
    
    private static string ComputeLocalAuthBody(string serverId, long gameId, string gameVersion, string modInfo, string channel, int userId, string handshakeKey)
    {
        // 本地实现认证体计算逻辑
        // 由于WebNexusApi被废弃，我们需要本地实现
        
        // 模拟认证体生成逻辑
        // 在实际实现中，这里应该有具体的加密/哈希算法
        var combinedData = $"{serverId}:{gameId}:{gameVersion}:{modInfo}:{channel}:{userId}:{handshakeKey}";
        var authBody = Convert.ToBase64String(Encoding.UTF8.GetBytes(combinedData));
        
        return authBody;
    }
}
