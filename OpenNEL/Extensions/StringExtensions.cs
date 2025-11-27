using System.Security.Cryptography;
using System.Text;

namespace OpenNEL.Extensions;

public static class StringExtensions
{
	public static string ToSha256(this string value)
	{
		return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
	}
}
