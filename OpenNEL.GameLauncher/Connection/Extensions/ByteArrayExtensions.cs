namespace OpenNEL.GameLauncher.Connection.Extensions;

public static class ByteArrayExtensions
{
    public static byte[] Xor(this byte[] content, byte[] key)
    {
        if (content.Length != key.Length)
        {
            throw new ArgumentException("Key length must be equal to content length.");
        }
        byte[] array = new byte[content.Length];
        for (int i = 0; i < content.Length; i++)
        {
            array[i] = (byte)(content[i] ^ key[i]);
        }
        return array;
    }

    public static byte[] CombineWith(this byte[]? first, byte[]? second)
    {
        ArgumentNullException.ThrowIfNull(first, nameof(first));
        ArgumentNullException.ThrowIfNull(second, nameof(second));
        return first.Concat(second).ToArray();
    }
}
