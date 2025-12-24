using System;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;

namespace OpenNEL.SDK.Extensions;

public static class CryptExtensions
{
	public static string ToSha1(this MemoryStream data)
	{
		using SHA1 sHA = SHA1.Create();
		byte[] array = sHA.ComputeHash(data);
		Array.Reverse(array);
		BigInteger bigInteger = new BigInteger(array);
		if (bigInteger < 0L)
		{
			return "-" + (-bigInteger).ToString("x").TrimStart('0');
		}
		return bigInteger.ToString("x").TrimStart('0');
	}
}
