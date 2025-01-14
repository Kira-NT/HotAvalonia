#if NETSTANDARD2_0
global using BitConverter = System.Compat.BitConverter;

using System.Runtime.InteropServices;

namespace System.Compat;

/// <inheritdoc cref="System.BitConverter"/>
internal static class BitConverter
{
    /// <inheritdoc cref="System.BitConverter.ToBoolean"/>
    public static bool ToBoolean(ReadOnlySpan<byte> value) => MemoryMarshal.Read<bool>(value);

    /// <inheritdoc cref="System.BitConverter.ToChar"/>
    public static char ToChar(ReadOnlySpan<byte> value) => MemoryMarshal.Read<char>(value);

    /// <inheritdoc cref="System.BitConverter.ToInt16"/>
    public static short ToInt16(ReadOnlySpan<byte> value) => MemoryMarshal.Read<short>(value);

    /// <inheritdoc cref="System.BitConverter.ToUInt16"/>
    public static ushort ToUInt16(ReadOnlySpan<byte> value) => MemoryMarshal.Read<ushort>(value);

    /// <inheritdoc cref="System.BitConverter.ToInt32"/>
    public static int ToInt32(ReadOnlySpan<byte> value) => MemoryMarshal.Read<int>(value);

    /// <inheritdoc cref="System.BitConverter.ToUInt32"/>
    public static uint ToUInt32(ReadOnlySpan<byte> value) => MemoryMarshal.Read<uint>(value);

    /// <inheritdoc cref="System.BitConverter.ToInt64"/>
    public static long ToInt64(ReadOnlySpan<byte> value) => MemoryMarshal.Read<long>(value);

    /// <inheritdoc cref="System.BitConverter.ToUInt64"/>
    public static ulong ToUInt64(ReadOnlySpan<byte> value) => MemoryMarshal.Read<ulong>(value);

    /// <inheritdoc cref="System.BitConverter.ToSingle"/>
    public static float ToSingle(ReadOnlySpan<byte> value) => MemoryMarshal.Read<float>(value);

    /// <inheritdoc cref="System.BitConverter.ToDouble"/>
    public static double ToDouble(ReadOnlySpan<byte> value) => MemoryMarshal.Read<double>(value);

    /// <inheritdoc cref="System.BitConverter.ToString(byte[])"/>
    public static string ToString(byte[] value) => System.BitConverter.ToString(value);

    /// <inheritdoc cref="System.BitConverter.GetBytes(bool)"/>
    public static bool TryWriteBytes(Span<byte> destination, bool value)
    {
        if (sizeof(bool) > destination.Length)
            return false;

        byte[] bytes = System.BitConverter.GetBytes(value);
        bytes.CopyTo(destination);
        return true;
    }

    /// <inheritdoc cref="System.BitConverter.GetBytes(char)"/>
    public static bool TryWriteBytes(Span<byte> destination, char value)
    {
        if (sizeof(char) > destination.Length)
            return false;

        byte[] bytes = System.BitConverter.GetBytes(value);
        bytes.CopyTo(destination);
        return true;
    }

    /// <inheritdoc cref="System.BitConverter.GetBytes(short)"/>
    public static bool TryWriteBytes(Span<byte> destination, short value)
    {
        if (sizeof(short) > destination.Length)
            return false;

        byte[] bytes = System.BitConverter.GetBytes(value);
        bytes.CopyTo(destination);
        return true;
    }

    /// <inheritdoc cref="System.BitConverter.GetBytes(ushort)"/>
    public static bool TryWriteBytes(Span<byte> destination, ushort value)
    {
        if (sizeof(ushort) > destination.Length)
            return false;

        byte[] bytes = System.BitConverter.GetBytes(value);
        bytes.CopyTo(destination);
        return true;
    }

    /// <inheritdoc cref="System.BitConverter.GetBytes(int)"/>
    public static bool TryWriteBytes(Span<byte> destination, int value)
    {
        if (sizeof(int) > destination.Length)
            return false;

        byte[] bytes = System.BitConverter.GetBytes(value);
        bytes.CopyTo(destination);
        return true;
    }

    /// <inheritdoc cref="System.BitConverter.GetBytes(uint)"/>
    public static bool TryWriteBytes(Span<byte> destination, uint value)
    {
        if (sizeof(uint) > destination.Length)
            return false;

        byte[] bytes = System.BitConverter.GetBytes(value);
        bytes.CopyTo(destination);
        return true;
    }

    /// <inheritdoc cref="System.BitConverter.GetBytes(long)"/>
    public static bool TryWriteBytes(Span<byte> destination, long value)
    {
        if (sizeof(long) > destination.Length)
            return false;

        byte[] bytes = System.BitConverter.GetBytes(value);
        bytes.CopyTo(destination);
        return true;
    }

    /// <inheritdoc cref="System.BitConverter.GetBytes(ulong)"/>
    public static bool TryWriteBytes(Span<byte> destination, ulong value)
    {
        if (sizeof(ulong) > destination.Length)
            return false;

        byte[] bytes = System.BitConverter.GetBytes(value);
        bytes.CopyTo(destination);
        return true;
    }

    /// <inheritdoc cref="System.BitConverter.GetBytes(float)"/>
    public static bool TryWriteBytes(Span<byte> destination, float value)
    {
        if (sizeof(float) > destination.Length)
            return false;

        byte[] bytes = System.BitConverter.GetBytes(value);
        bytes.CopyTo(destination);
        return true;
    }

    /// <inheritdoc cref="System.BitConverter.GetBytes(double)"/>
    public static bool TryWriteBytes(Span<byte> destination, double value)
    {
        if (sizeof(double) > destination.Length)
            return false;

        byte[] bytes = System.BitConverter.GetBytes(value);
        bytes.CopyTo(destination);
        return true;
    }
}
#endif
