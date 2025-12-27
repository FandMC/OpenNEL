using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Codexus.OpenSDK.Entities.Yggdrasil;
using Codexus.OpenSDK.Yggdrasil;
using OpenNEL.GameLauncher.Connection.ChaCha;
using OpenNEL.GameLauncher.Connection.Entities;
using OpenNEL.GameLauncher.Connection.Extensions;
using OpenNEL.Core.Utils;
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
        Func<string, string, int, string, byte[], string, string, byte[]>? buildEstablishing = null, 
        Func<string, ChaChaOfSalsa, string, long, string, string, string, int, byte[], string, byte[]>? buildJoinServerMessage = null)
    {
        buildEstablishing ??= DefaultBuildEstablishing;
        buildJoinServerMessage ??= DefaultBuildJoinServerMessage;
        
        var crcSalt = await CrcSalt.Compute();

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
            
            byte[] establishMsg = buildEstablishing(nexusToken, gameVersion, userId, userToken, context, "netease", crcSalt);
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
            
            byte[] joinMsg = buildJoinServerMessage(nexusToken, encryptCipher, serverId, long.Parse(gameId), gameVersion, modInfo, "netease", userId, remoteKey, crcSalt);
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
            if (ex.StatusCode == HttpStatusCode.Unauthorized)
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

    private static byte[] DefaultBuildEstablishing(string nexusToken, string gameVersion, int userId, string userToken, byte[] context, string channel, string crcSalt)
    {
        Log.Information("Building establishing message");
        
        var md5Pair = Md5Mapping.GetMd5FromGameVersion(gameVersion);
        
        var userProfile = new UserProfile { UserId = userId, UserToken = userToken };
        
        var gameProfile = new GameProfile
        {
            GameVersion = gameVersion,
            User = userProfile,
            GameId = userId.ToString(),
            BootstrapMd5 = md5Pair.BootstrapMd5,
            DatFileMd5 = md5Pair.DatFileMd5,
            Mods = new ModList()
        };
        
        var yggdrasilData = new YggdrasilData
        {
            LauncherVersion = gameVersion,
            Channel = channel,
            CrcSalt = crcSalt
        };
        
        var generator = new Codexus.OpenSDK.Generator.YggdrasilGenerator(yggdrasilData);
        
        var loginSeed = new byte[16];
        var signContent = new byte[256];
        if (context.Length >= 272)
        {
            Array.Copy(context, 0, loginSeed, 0, 16);
            Array.Copy(context, 16, signContent, 0, 256);
        }
        
        var initMessage = generator.GenerateInitializeMessage(gameProfile, loginSeed, signContent);
        
        return initMessage;
    }

    private static byte[] DefaultBuildJoinServerMessage(string nexusToken, ChaChaOfSalsa cipher, string serverId, long gameId, string gameVersion, string modInfo, string channel, int userId, byte[] handshakeKey, string crcSalt)
    {
        Log.Information("Building join server message");
        
        var md5Pair = Md5Mapping.GetMd5FromGameVersion(gameVersion);
        
        var userProfile = new UserProfile { UserId = userId, UserToken = nexusToken };
        
        var gameProfile = new GameProfile
        {
            GameId = gameId.ToString(),
            GameVersion = gameVersion,
            BootstrapMd5 = md5Pair.BootstrapMd5,
            DatFileMd5 = md5Pair.DatFileMd5,
            User = userProfile,
            Mods = new ModList()
        };
        
        var yggdrasilData = new YggdrasilData
        {
            LauncherVersion = gameVersion,
            Channel = channel,
            CrcSalt = crcSalt
        };
        
        var generator = new Codexus.OpenSDK.Generator.YggdrasilGenerator(yggdrasilData);
        
        var joinMessage = generator.GenerateJoinMessage(gameProfile, serverId, handshakeKey);
        
        return cipher.PackMessage(9, joinMessage);
    }
}
