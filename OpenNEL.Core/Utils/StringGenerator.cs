using System.Text;

namespace OpenNEL.Core.Utils;

public static class StringGenerator
{
    private static readonly Random Random = new();

    public static string GenerateHexString(int length)
    {
        var bytes = new byte[length];
        Random.NextBytes(bytes);
        return Convert.ToHexString(bytes);
    }

    public static string GenerateRandomString(int length, bool includeNumbers = true, bool includeUppercase = true, bool includeLowercase = true)
    {
        if (length <= 0)
        {
            throw new ArgumentException("Length must be greater than 0", nameof(length));
        }
        if (!includeNumbers && !includeUppercase && !includeLowercase)
        {
            throw new ArgumentException("Must include at least one character type");
        }

        var chars = new StringBuilder();
        if (includeNumbers) chars.Append("0123456789");
        if (includeUppercase) chars.Append("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        if (includeLowercase) chars.Append("abcdefghijklmnopqrstuvwxyz");

        var charArray = chars.ToString();
        var result = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            result.Append(charArray[Random.Next(charArray.Length)]);
        }
        return result.ToString();
    }

    public static string GenerateRandomMacAddress(string separator = ":", bool uppercase = true)
    {
        var mac = new byte[6];
        Random.NextBytes(mac);
        mac[0] = (byte)((mac[0] & 0xFE) | 0x02);

        var format = uppercase ? "X2" : "x2";
        return string.Join(separator,
            mac[0].ToString(format),
            mac[1].ToString(format),
            mac[2].ToString(format),
            mac[3].ToString(format),
            mac[4].ToString(format),
            mac[5].ToString(format));
    }
    
    public static string GetRandomString(int length, string chars)
    {
        var result = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            result.Append(chars[Random.Next(chars.Length)]);
        }
        return result.ToString();
    }
}
