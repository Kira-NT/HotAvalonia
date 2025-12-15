#if NETSTANDARD2_0
using System.Runtime.InteropServices;

namespace System;

internal static class BitConverterPolyfill
{
    extension(BitConverter)
    {
        public static bool ToBoolean(ReadOnlySpan<byte> value) => MemoryMarshal.Read<bool>(value);

        public static char ToChar(ReadOnlySpan<byte> value) => MemoryMarshal.Read<char>(value);

        public static short ToInt16(ReadOnlySpan<byte> value) => MemoryMarshal.Read<short>(value);

        public static ushort ToUInt16(ReadOnlySpan<byte> value) => MemoryMarshal.Read<ushort>(value);

        public static int ToInt32(ReadOnlySpan<byte> value) => MemoryMarshal.Read<int>(value);

        public static uint ToUInt32(ReadOnlySpan<byte> value) => MemoryMarshal.Read<uint>(value);

        public static long ToInt64(ReadOnlySpan<byte> value) => MemoryMarshal.Read<long>(value);

        public static ulong ToUInt64(ReadOnlySpan<byte> value) => MemoryMarshal.Read<ulong>(value);

        public static float ToSingle(ReadOnlySpan<byte> value) => MemoryMarshal.Read<float>(value);

        public static double ToDouble(ReadOnlySpan<byte> value) => MemoryMarshal.Read<double>(value);

        public static bool TryWriteBytes(Span<byte> destination, bool value)
        {
            if (sizeof(bool) > destination.Length)
                return false;

            byte[] bytes = BitConverter.GetBytes(value);
            bytes.CopyTo(destination);
            return true;
        }

        public static bool TryWriteBytes(Span<byte> destination, char value)
        {
            if (sizeof(char) > destination.Length)
                return false;

            byte[] bytes = BitConverter.GetBytes(value);
            bytes.CopyTo(destination);
            return true;
        }

        public static bool TryWriteBytes(Span<byte> destination, short value)
        {
            if (sizeof(short) > destination.Length)
                return false;

            byte[] bytes = BitConverter.GetBytes(value);
            bytes.CopyTo(destination);
            return true;
        }

        public static bool TryWriteBytes(Span<byte> destination, ushort value)
        {
            if (sizeof(ushort) > destination.Length)
                return false;

            byte[] bytes = BitConverter.GetBytes(value);
            bytes.CopyTo(destination);
            return true;
        }

        public static bool TryWriteBytes(Span<byte> destination, int value)
        {
            if (sizeof(int) > destination.Length)
                return false;

            byte[] bytes = BitConverter.GetBytes(value);
            bytes.CopyTo(destination);
            return true;
        }

        public static bool TryWriteBytes(Span<byte> destination, uint value)
        {
            if (sizeof(uint) > destination.Length)
                return false;

            byte[] bytes = BitConverter.GetBytes(value);
            bytes.CopyTo(destination);
            return true;
        }

        public static bool TryWriteBytes(Span<byte> destination, long value)
        {
            if (sizeof(long) > destination.Length)
                return false;

            byte[] bytes = BitConverter.GetBytes(value);
            bytes.CopyTo(destination);
            return true;
        }

        public static bool TryWriteBytes(Span<byte> destination, ulong value)
        {
            if (sizeof(ulong) > destination.Length)
                return false;

            byte[] bytes = BitConverter.GetBytes(value);
            bytes.CopyTo(destination);
            return true;
        }

        public static bool TryWriteBytes(Span<byte> destination, float value)
        {
            if (sizeof(float) > destination.Length)
                return false;

            byte[] bytes = BitConverter.GetBytes(value);
            bytes.CopyTo(destination);
            return true;
        }

        public static bool TryWriteBytes(Span<byte> destination, double value)
        {
            if (sizeof(double) > destination.Length)
                return false;

            byte[] bytes = BitConverter.GetBytes(value);
            bytes.CopyTo(destination);
            return true;
        }
    }
}
#endif
