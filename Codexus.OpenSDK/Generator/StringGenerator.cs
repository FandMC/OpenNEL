using System.Text;

namespace Codexus.OpenSDK.Generator;

public static class StringGenerator
{
    private const string Numbers = "0123456789";
    private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";

    private static readonly Random Random = new();

    public static string GenerateHexString(int length)
    {
        var bytes = new byte[length];
        Random.NextBytes(bytes);
        return BitConverter.ToString(bytes);
    }

    public static string GenerateRandomString(
        int length,
        bool includeNumbers = true,
        bool includeUppercase = true,
        bool includeLowercase = true)
    {
        if (length <= 0) throw new ArgumentException("Length must be greater than 0", nameof(length));

        if (!includeNumbers && !includeUppercase && !includeLowercase)
            throw new ArgumentException("Must include at least one character type",
                nameof(includeNumbers) + ", " + nameof(includeUppercase) + ", " + nameof(includeLowercase));

        var charPool = new StringBuilder();

        if (includeNumbers) charPool.Append(Numbers);
        if (includeUppercase) charPool.Append(Uppercase);
        if (includeLowercase) charPool.Append(Lowercase);

        var poolLength = charPool.Length;
        var result = new StringBuilder(length);

        for (var i = 0; i < length; i++) result.Append(charPool[Random.Next(poolLength)]);

        return result.ToString();
    }

    public static string GenerateRandomMacAddress(string separator = ":", bool uppercase = true)
    {
        var mac = new byte[6];
        Random.NextBytes(mac);

        mac[0] = (byte)(mac[0] & 0xFE);
        mac[0] = (byte)(mac[0] | 0x02);

        var format = uppercase ? "X2" : "x2";

        return string.Join(separator,
            mac[0].ToString(format),
            mac[1].ToString(format),
            mac[2].ToString(format),
            mac[3].ToString(format),
            mac[4].ToString(format),
            mac[5].ToString(format));
    }
}