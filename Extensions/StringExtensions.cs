using System.Security.Cryptography;
using System.Text;

namespace OpenNEL.Extensions;

public static class StringExtensions
{
    public static string ToSha256(this string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(bytes);
        
        // Compatible implementation for older .NET versions or missing APIs
        var sb = new StringBuilder();
        foreach (var b in hash)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}
