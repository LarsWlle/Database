using System.Runtime.InteropServices;

namespace Database.Util;

public static class Extensions {
    public static byte[] ParseToBytes<T>(this T value) where T : struct {
        int size = Marshal.SizeOf<T>();
        byte[] bytes = new byte[size];
        dynamic v = value;
        for (int i = 0; i < size; i++) bytes[i] = (byte) (v >> (8 * (size - 1 - i)));

        return bytes;
    }

    public static T ParseToNumber<T>(this byte[] bytes) where T : struct {
        dynamic value = 0;
        int size = bytes.Length;
        for (int i = 0; i < size; i++) value |= (dynamic) bytes[i] << (8 * (size - i - 1));

        return (T) value;
    }

    public static T ParseToNumber<T>(this IEnumerable<byte> bytes) where T : struct => ParseToNumber<T>(bytes.ToArray());

    public static byte[] HexStringToBytes(this string hex) => Enumerable.Range(0, hex.Length / 2)
        .Select(i => Convert.ToByte(hex.Substring(i * 2, 2), 16))
        .ToArray();
    
    public static string ToFixedLength(this string str, int length) => (str.Length <= length ? str.PadRight(length) : str[..length]); 
}