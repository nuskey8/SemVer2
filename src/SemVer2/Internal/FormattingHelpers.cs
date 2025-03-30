// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers.Text
{
    internal static partial class FormattingHelpers
    {
        // Based on do_count_digits from https://github.com/fmtlib/fmt/blob/662adf4f33346ba9aba8b072194e319869ede54a/include/fmt/format.h#L1124
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountDigits(ulong value)
        {
            // Map the log2(value) to a power of 10.
            ReadOnlySpan<byte> log2ToPow10 =
            [
                1,  1,  1,  2,  2,  2,  3,  3,  3,  4,  4,  4,  4,  5,  5,  5,
                6,  6,  6,  7,  7,  7,  7,  8,  8,  8,  9,  9,  9,  10, 10, 10,
                10, 11, 11, 11, 12, 12, 12, 13, 13, 13, 13, 14, 14, 14, 15, 15,
                15, 16, 16, 16, 16, 17, 17, 17, 18, 18, 18, 19, 19, 19, 19, 20
            ];
            Debug.Assert(log2ToPow10.Length == 64);

            // TODO: Replace with log2ToPow10[BitOperations.Log2(value)] once https://github.com/dotnet/runtime/issues/79257 is fixed
#if NETCOREAPP3_0_OR_GREATER
            nint elementOffset = Unsafe.Add(ref MemoryMarshal.GetReference(log2ToPow10), BitOperations.Log2(value));
#else
            nint elementOffset = Unsafe.Add(ref MemoryMarshal.GetReference(log2ToPow10), Log2SoftwareFallback(value));
#endif

            // Read the associated power of 10.
            ReadOnlySpan<ulong> powersOf10 =
            [
                0, // unused entry to avoid needing to subtract
                0,
                10,
                100,
                1000,
                10000,
                100000,
                1000000,
                10000000,
                100000000,
                1000000000,
                10000000000,
                100000000000,
                1000000000000,
                10000000000000,
                100000000000000,
                1000000000000000,
                10000000000000000,
                100000000000000000,
                1000000000000000000,
                10000000000000000000,
            ];
            Debug.Assert((elementOffset + 1) <= powersOf10.Length);
            ulong powerOf10 = Unsafe.Add(ref MemoryMarshal.GetReference(powersOf10), elementOffset);

            // Return the number of digits based on the power of 10, shifted by 1
            // if it falls below the threshold.
            int index = (int)elementOffset;
            return index - (value < powerOf10 ? 1 : 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountDigits(uint value)
        {
            // Algorithm based on https://lemire.me/blog/2021/06/03/computing-the-number-of-digits-of-an-integer-even-faster.
            ReadOnlySpan<long> table =
            [
                4294967296,
                8589934582,
                8589934582,
                8589934582,
                12884901788,
                12884901788,
                12884901788,
                17179868184,
                17179868184,
                17179868184,
                21474826480,
                21474826480,
                21474826480,
                21474826480,
                25769703776,
                25769703776,
                25769703776,
                30063771072,
                30063771072,
                30063771072,
                34349738368,
                34349738368,
                34349738368,
                34349738368,
                38554705664,
                38554705664,
                38554705664,
                41949672960,
                41949672960,
                41949672960,
                42949672960,
                42949672960,
            ];
            Debug.Assert(table.Length == 32, "Every result of uint.Log2(value) needs a long entry in the table.");

            // TODO: Replace with table[uint.Log2(value)] once https://github.com/dotnet/runtime/issues/79257 is fixed
#if NET7_0_OR_GREATER
            long tableValue = Unsafe.Add(ref MemoryMarshal.GetReference(table), uint.Log2(value));
#elif NETCOREAPP3_0_OR_GREATER
            long tableValue = Unsafe.Add(ref MemoryMarshal.GetReference(table), BitOperations.Log2(value));
#else
            long tableValue = Unsafe.Add(ref MemoryMarshal.GetReference(table), Log2SoftwareFallback(value));
#endif

            return (int)((value + tableValue) >> 32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountHexDigits(ulong value)
        {
            // The number of hex digits is log16(value) + 1, or log2(value) / 4 + 1
#if NETCOREAPP3_0_OR_GREATER                         
            return (BitOperations.Log2(value) >> 2) + 1;
#else
            return (Log2SoftwareFallback(value) >> 2) + 1;
#endif
        }

        // Counts the number of trailing '0' digits in a decimal number.
        // e.g., value =      0 => retVal = 0, valueWithoutTrailingZeros = 0
        //       value =   1234 => retVal = 0, valueWithoutTrailingZeros = 1234
        //       value = 320900 => retVal = 2, valueWithoutTrailingZeros = 3209
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountDecimalTrailingZeros(uint value, out uint valueWithoutTrailingZeros)
        {
            int zeroCount = 0;

            if (value != 0)
            {
                while (true)
                {
                    uint temp = value / 10;
                    if (value != (temp * 10))
                    {
                        break;
                    }

                    value = temp;
                    zeroCount++;
                }
            }

            valueWithoutTrailingZeros = value;
            return zeroCount;
        }

        // BitOperations

        static ReadOnlySpan<byte> Log2DeBruijn => // 32
        [
            00, 09, 01, 10, 13, 21, 02, 29,
            11, 14, 16, 18, 22, 25, 03, 30,
            08, 12, 20, 28, 15, 17, 24, 07,
            19, 27, 23, 06, 26, 05, 04, 31
        ];

        static int Log2SoftwareFallback(ulong value)
        {
            uint hi = (uint)(value >> 32);

            if (hi == 0)
            {
                return Log2SoftwareFallback((uint)value);
            }

            return 32 + Log2SoftwareFallback(hi);
        }

        static int Log2SoftwareFallback(uint value)
        {
            // No AggressiveInlining due to large method size
            // Has conventional contract 0->0 (Log(0) is undefined)

            // Fill trailing zeros with ones, eg 00010010 becomes 00011111
            value |= value >> 01;
            value |= value >> 02;
            value |= value >> 04;
            value |= value >> 08;
            value |= value >> 16;

            // uint.MaxValue >> 27 is always in range [0 - 31] so we use Unsafe.AddByteOffset to avoid bounds check
            return Unsafe.AddByteOffset(
                // Using deBruijn sequence, k=2, n=5 (2^5=32) : 0b_0000_0111_1100_0100_1010_1100_1101_1101u
                ref MemoryMarshal.GetReference(Log2DeBruijn),
                // uint|long -> IntPtr cast on 32-bit platforms does expensive overflow checks not needed here
                (IntPtr)(int)((value * 0x07C4ACDDu) >> 27));
        }
    }
}