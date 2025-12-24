using System.Security.Cryptography;
using System.Text;

namespace OpenNEL.Core.Extensions;

public static class CryptoExtensions
{
    public static string EncodeMd5(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }
        return Encoding.UTF8.GetBytes(input).EncodeMd5();
    }

    public static string EncodeMd5(this byte[] inputBytes)
    {
        return MD5.HashData(inputBytes).EncodeHex();
    }

    public static string EncodeBase64(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
    }

    public static string EncodeHex(this byte[] input)
    {
        return Convert.ToHexString(input).Replace("-", "").ToLower();
    }

    public static byte[] DecodeHex(this string input)
    {
        return Convert.FromHexString(input);
    }

    public static byte[] EncodeAes(this string input, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;
        using var encryptor = aes.CreateEncryptor();
        var bytes = Encoding.UTF8.GetBytes(input);
        return encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
    }
}
