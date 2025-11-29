using System.Security.Cryptography;

namespace OpenNEL.Extensions;

public static class ByteArrayExtensions
{
	public static ECDiffieHellmanPublicKey ToEcDiffieHellmanPublicKey(this byte[] entity)
	{
		using ECDiffieHellman eCDiffieHellman = ECDiffieHellman.Create(EcCurveExtensions.DefaultCurve);
		eCDiffieHellman.ImportSubjectPublicKeyInfo(entity, out var _);
		return eCDiffieHellman.PublicKey;
	}
}
