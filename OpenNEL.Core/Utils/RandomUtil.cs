using System.Security.Cryptography;
using System.Text;

namespace OpenNEL.Core.Utils;

public static class RandomUtil
{
    public static string GetRandomString(int length, string? chars = null)
    {
        if (length <= 0)
        {
            return string.Empty;
        }
        if (string.IsNullOrEmpty(chars))
        {
            chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghizklmnopqrstuvwxyz0123456789";
        }
        var builder = new StringBuilder(length);
        var array = new byte[length];
        RandomNumberGenerator.Fill(array);
        for (int i = 0; i < length; i++)
        {
            int index = array[i] % chars.Length;
            builder.Append(chars[index]);
        }
        return builder.ToString();
    }

    public static string GenerateSessionId()
    {
        return "captchaReq" + GetRandomString(16);
    }
}
