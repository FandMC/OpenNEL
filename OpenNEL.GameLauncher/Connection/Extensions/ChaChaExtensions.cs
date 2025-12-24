using System.IO.Hashing;
using OpenNEL.GameLauncher.Connection.ChaCha;

namespace OpenNEL.GameLauncher.Connection.Extensions;

public static class ChaChaExtensions
{
    public static byte[] PackMessage(this ChaChaOfSalsa cipher, byte type, byte[] data)
    {
        byte[] array = new byte[data.Length + 10];
        Array.Copy(BitConverter.GetBytes((short)(array.Length - 2)), 0, array, 0, 2);
        array[6] = type;
        array[7] = 136;
        array[8] = 136;
        array[9] = 136;
        Array.Copy(data, 0, array, 10, data.Length);
        Array.Copy(Crc32.Hash((ReadOnlySpan<byte>)array.AsSpan(6)), 0, array, 2, 4);
        cipher.ProcessBytes(array, 2, array.Length - 2, array, 2);
        return array;
    }

    public static (byte, byte[]) UnpackMessage(this ChaChaOfSalsa cipher, byte[] data)
    {
        cipher.ProcessBytes(data, 0, data.Length, data, 0);
        byte[] array = new byte[4];
        Crc32.Hash((ReadOnlySpan<byte>)data.AsSpan(4, data.Length - 4), (Span<byte>)array);
        for (int i = 0; i < 4; i++)
        {
            if (array[i] != data[i])
            {
                throw new Exception("Unpacking failed");
            }
        }
        byte[] array2 = new byte[data.Length - 8];
        Array.Copy(data, 8, array2, 0, array2.Length);
        return (data[4], array2);
    }
}
