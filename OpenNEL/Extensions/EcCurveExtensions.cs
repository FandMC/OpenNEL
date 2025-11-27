using System.Security.Cryptography;

namespace OpenNEL.Extensions;

public static class EcCurveExtensions
{
	public static readonly ECCurve DefaultCurve = ECCurve.NamedCurves.nistP256;
}
