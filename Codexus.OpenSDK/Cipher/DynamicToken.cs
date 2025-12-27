using System.Security.Cryptography;
using System.Text;
using Codexus.OpenSDK.Extensions;

namespace Codexus.OpenSDK.Cipher;

public static class DynamicToken
{
    private const string TokenSalt = "0eGsBkhl";

    private static readonly Aes Aes = Aes.Create();

    static DynamicToken()
    {
        Aes.Mode = CipherMode.CBC;
        Aes.Padding = PaddingMode.Zeros;
        Aes.KeySize = 128;
        Aes.BlockSize = 128;
        Aes.Key = "debbde3548928fab"u8.ToArray();
        Aes.IV = "afd4c5c5a7c456a1"u8.ToArray();
    }

    public static Dictionary<string, string> Compute(string requestPath, string sendBody, string userId,
        string userToken)
    {
        return Compute(requestPath, Encoding.UTF8.GetBytes(sendBody), userId, userToken);
    }

    private static Dictionary<string, string> Compute(string requestPath, byte[] sendBody,
        string userId,
        string userToken)
    {
        requestPath = requestPath.StartsWith('/') ? requestPath : $"/{requestPath}";

        using var stream = new MemoryStream();
        stream.Write(Encoding.UTF8.GetBytes(userToken.EncodeMd5().ToLowerInvariant()));
        stream.Write(sendBody);
        stream.Write(Encoding.UTF8.GetBytes(TokenSalt));
        stream.Write(Encoding.UTF8.GetBytes(requestPath));

        var secretMd5 = stream.ToArray().EncodeMd5().ToLowerInvariant();
        var secretBin = HexToBinary(secretMd5);
        secretBin = secretBin[6..] + secretBin[..6];

        var httpToken = Encoding.UTF8.GetBytes(secretMd5);
        ProcessBinaryBlock(secretBin, httpToken);

        var dynamicToken = (Convert.ToBase64String(httpToken, 0, 12) + "1")
            .Replace('+', 'm')
            .Replace('/', 'o');

        return new Dictionary<string, string>
        {
            ["user-id"] = userId,
            ["user-token"] = dynamicToken
        };
    }

    private static void ProcessBinaryBlock(string secretBin, byte[] httpToken)
    {
        for (var i = 0; i < secretBin.Length / 8; i++)
        {
            var block = secretBin.AsSpan(i * 8, Math.Min(8, secretBin.Length - i * 8));
            byte xorBuffer = 0;

            for (var j = 0; j < block.Length; j++)
                if (block[7 - j] == '1')
                    xorBuffer |= (byte)(1 << j);

            httpToken[i] ^= xorBuffer;
        }
    }

    private static string HexToBinary(string hexString)
    {
        var binaryBuilder = new StringBuilder();
        foreach (var binaryString in hexString.Select(hex => Convert.ToString(hex, 2).PadLeft(8, '0')))
            binaryBuilder.Append(binaryString);

        return binaryBuilder.ToString();
    }
}