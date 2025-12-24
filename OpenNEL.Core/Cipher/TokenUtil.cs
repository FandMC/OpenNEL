using System.Security.Cryptography;
using System.Text;
using OpenNEL.Core.Extensions;

namespace OpenNEL.Core.Cipher;

public static class TokenUtil
{
    private const string TokenSalt = "0eGsBkhl";

    private static readonly Aes Aes;

    static TokenUtil()
    {
        Aes = System.Security.Cryptography.Aes.Create();
        Aes.Mode = CipherMode.CBC;
        Aes.Padding = PaddingMode.Zeros;
        Aes.KeySize = 128;
        Aes.BlockSize = 128;
        Aes.Key = "debbde3548928fab"u8.ToArray();
        Aes.IV = "afd4c5c5a7c456a1"u8.ToArray();
    }

    public static Dictionary<string, string> ComputeHttpRequestToken(string requestPath, string sendBody, string userId, string userToken)
    {
        return ComputeHttpRequestToken(requestPath, Encoding.UTF8.GetBytes(sendBody), userId, userToken);
    }

    private static Dictionary<string, string> ComputeHttpRequestToken(string requestPath, byte[] sendBody, string userId, string userToken)
    {
        requestPath = requestPath.StartsWith('/') ? requestPath : "/" + requestPath;
        using var stream = new MemoryStream();
        stream.Write(Encoding.UTF8.GetBytes(userToken.EncodeMd5().ToLowerInvariant()));
        stream.Write(sendBody);
        stream.Write(Encoding.UTF8.GetBytes(TokenSalt));
        stream.Write(Encoding.UTF8.GetBytes(requestPath));
        
        var hash = stream.ToArray().EncodeMd5().ToLowerInvariant();
        var binary = HexToBinary(hash);
        binary = binary[6..] + binary[..6];
        
        var bytes = Encoding.UTF8.GetBytes(hash);
        ProcessBinaryBlock(binary, bytes);
        
        var value = (Convert.ToBase64String(bytes, 0, 12) + "1").Replace('+', 'm').Replace('/', 'o');
        return new Dictionary<string, string>
        {
            ["user-id"] = userId,
            ["user-token"] = value
        };
    }

    private static void ProcessBinaryBlock(string secretBin, byte[] httpToken)
    {
        for (var i = 0; i < secretBin.Length / 8; i++)
        {
            var span = secretBin.AsSpan(i * 8, Math.Min(8, secretBin.Length - i * 8));
            byte b = 0;
            for (var j = 0; j < span.Length; j++)
            {
                if (span[7 - j] == '1')
                {
                    b |= (byte)(1 << j);
                }
            }
            httpToken[i] ^= b;
        }
    }

    private static string HexToBinary(string hexString)
    {
        var sb = new StringBuilder();
        foreach (var item in hexString.Select(hex => Convert.ToString(hex, 2).PadLeft(8, '0')))
        {
            sb.Append(item);
        }
        return sb.ToString();
    }

    public static string GenerateEncryptToken(string userToken)
    {
        var prefix = Utils.StringGenerator.GetRandomString(8, "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789").ToUpper();
        var suffix = Utils.StringGenerator.GetRandomString(8, "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789").ToUpper();
        var data = prefix + userToken + suffix;
        var bytes = Encoding.ASCII.GetBytes(data);
        return Convert.ToHexString(Aes.CreateEncryptor().TransformFinalBlock(bytes, 0, bytes.Length)).ToUpper();
    }
}
