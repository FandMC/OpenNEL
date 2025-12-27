using System.Security.Cryptography;
using System.Text;
using OpenNEL.Core.Utils;

namespace OpenNEL.Core.Cipher;

public static class HttpUtil
{
    private const string SKeys = "MK6mipwmOUedplb6,OtEylfId6dyhrfdn,VNbhn5mvUaQaeOo9,bIEoQGQYjKd02U0J,fuaJrPwaH2cfXXLP,LEkdyiroouKQ4XN1,jM1h27H4UROu427W,DhReQada7gZybTDk,ZGXfpSTYUvcdKqdY,AZwKf7MWZrJpGR5W,amuvbcHw38TcSyPU,SI4QotspbjhyFdT0,VP4dhjKnDGlSJtbB,UXDZx4KhZywQ2tcn,NIK73ZNvNqzva4kd,WeiW7qU766Q1YQZI";

    private static Aes Aes
    {
        get
        {
            var aes = Aes.Create();
            aes.Padding = PaddingMode.None;
            return aes;
        }
    }

    private static byte[][] HttpKeys => SKeys.Split(',')
        .Select(skey => Encoding.GetEncoding("us-ascii").GetBytes(skey))
        .ToArray();

    public static byte[] HttpEncrypt(byte[] bodyIn)
    {
        var array = new byte[(int)Math.Ceiling((bodyIn.Length + 16) / 16.0) * 16];
        Array.Copy(bodyIn, array, bodyIn.Length);
        
        var bytes = Encoding.ASCII.GetBytes(StringGenerator.GenerateRandomString(16, includeNumbers: false));
        for (var i = 0; i < bytes.Length; i++)
        {
            array[i + bodyIn.Length] = bytes[i];
        }
        
        var keyIndex = (byte)((Random.Shared.Next(0, HttpKeys.Length - 1) << 4) | 2);
        var encrypted = Aes.CreateEncryptor(HttpKeys[(keyIndex >> 4) & 0xF], bytes).TransformFinalBlock(array, 0, array.Length);
        
        var result = new byte[16 + encrypted.Length + 1];
        Array.Copy(bytes, result, 16);
        Array.Copy(encrypted, 0, result, 16, encrypted.Length);
        result[^1] = keyIndex;
        return result;
    }

    public static byte[]? HttpDecrypt(byte[] body)
    {
        if (body.Length < 18)
        {
            return null;
        }
        
        var encrypted = body.Skip(16).Take(body.Length - 1 - 16).ToArray();
        var decrypted = Aes.CreateDecryptor(HttpKeys[(body[^1] >> 4) & 0xF], body.Take(16).ToArray())
            .TransformFinalBlock(encrypted, 0, encrypted.Length);
        
        var count = 0;
        var index = decrypted.Length - 1;
        while (count < 16)
        {
            if (decrypted[index--] != 0)
            {
                count++;
            }
        }
        return decrypted.Take(index + 1).ToArray();
    }
}
