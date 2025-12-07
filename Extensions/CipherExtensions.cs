using System;
using System.IO;
using System.Security.Cryptography;

namespace OpenNEL.Extensions;

public static class CipherExtensions
{
	public static byte[] Decrypt(this byte[] payload, byte[]? key)
	{
		if (payload == null || payload.Length < 16)
		{
			throw new ArgumentException("Payload is null or too short to contain IV", "payload");
		}
		if (key == null || key.Length != 32)
		{
			throw new ArgumentException("Key must be 32 bytes (256 bits) for AES-256", "key");
		}
		byte[] array = new byte[16];
		Array.Copy(payload, 0, array, 0, array.Length);
		byte[] array2 = new byte[payload.Length - array.Length];
		Array.Copy(payload, array.Length, array2, 0, array2.Length);
		using Aes aes = Aes.Create();
		aes.Key = key;
		aes.IV = array;
		aes.Mode = CipherMode.CBC;
		aes.Padding = PaddingMode.PKCS7;
		using MemoryStream memoryStream = new MemoryStream();
		using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write))
		{
			cryptoStream.Write(array2, 0, array2.Length);
			cryptoStream.FlushFinalBlock();
		}
		return memoryStream.ToArray();
	}

	public static byte[] Encrypt(this byte[] payload, byte[]? key)
	{
		ArgumentNullException.ThrowIfNull(payload, "payload");
		if (key == null || key.Length != 32)
		{
			throw new ArgumentException("Key must be 32 bytes (256 bits) for AES-256", "key");
		}
		using Aes aes = Aes.Create();
		aes.Key = key;
		aes.GenerateIV();
		aes.Mode = CipherMode.CBC;
		aes.Padding = PaddingMode.PKCS7;
		using MemoryStream memoryStream = new MemoryStream();
		memoryStream.Write(aes.IV, 0, aes.IV.Length);
		using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
		{
			cryptoStream.Write(payload, 0, payload.Length);
			cryptoStream.FlushFinalBlock();
		}
		return memoryStream.ToArray();
	}
}
