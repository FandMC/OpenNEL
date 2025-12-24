using System.Reflection;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;

namespace OpenNEL.GameLauncher.Connection.ChaCha;

public sealed class ChaChaOfSalsa : ChaCha7539Engine
{
    private static readonly FieldInfo RoundsField = typeof(Salsa20Engine).GetField("rounds", BindingFlags.NonPublic | BindingFlags.Instance)!;
    
    public override string AlgorithmName => $"ChaCha{GetRounds()}";

    public ChaChaOfSalsa(byte[] key, byte[] iv, bool encryption, int rounds = 8)
    {
        SetRounds(rounds);
        Init(encryption, new ParametersWithIV(new KeyParameter(key), iv));
    }

    private void SetRounds(int rounds)
    {
        RoundsField.SetValue(this, rounds);
    }

    private int GetRounds()
    {
        return (int)(RoundsField.GetValue(this) ?? 8);
    }
}
