using System.Security.Cryptography;
using System.Text;

namespace Codexus.OpenSDK.Extensions;

public static class MPayExtensions
{
    extension(byte[] input)
    {
        public string EncodeMd5()
        {
            var hashBytes = MD5.HashData(input);
            return hashBytes.EncodeHex();
        }

        public string EncodeHex()
        {
            return Convert.ToHexString(input).Replace("-", "").ToLower();
        }
    }

    extension(string input)
    {
        public byte[] AesEncrypt(byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            var inputBytes = Encoding.UTF8.GetBytes(input);

            return encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
        }

        public byte[] DecodeHex()
        {
            return Convert.FromHexString(input);
        }

        public string EncodeMd5()
        {
            var inputBytes = Encoding.UTF8.GetBytes(input);
            return inputBytes.EncodeMd5();
        }

        public string EncodeBase64()
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            var inputBytes = Encoding.UTF8.GetBytes(input);
            var base64Bytes = Convert.ToBase64String(inputBytes);
            return base64Bytes;
        }
    }
}