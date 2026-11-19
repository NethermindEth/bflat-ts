// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Byte swaps, single-bit operations, shift-and-add forms and Log2.
//
// With the Zba, Zbb and Zbs extensions the compiler uses rev8 for a byte
// swap, bset/bclr/binv/bext for single bits, sh1add/sh2add/sh3add and their
// .uw forms, add.uw and slli.uw for a scaled or zero-extended index, and
// clz for BitOperations.Log2. What can go wrong: a 32-bit byte swap that is
// not sign-extended afterwards or that swaps 8 bytes of a 4-byte value, a
// single-bit instruction used for a 32-bit operation (it reads 6 bits of
// the index where 5 count, and bit 31 changes the sign extension), a
// shift-and-add that swallows a checked multiplication or a checked cast
// and so loses the overflow check, a slli.uw with a shift amount of 64 or
// more, and a Log2 that is wrong for 0, for 1 or for the top bit.
//
// Without those extensions (the default for zkVM guests other than ZisK)
// the same source runs through the base instructions; the expected values
// are the same. They are computed bit by bit, without optimization.
//
// A failed check prints one line; the run ends with ALL PASS or a FAIL
// count, and the guest returns 1 when anything failed.
using System;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;

unsafe class Program
{
    const MethodImplOptions NI = MethodImplOptions.NoInlining;
    const MethodImplOptions NoOpt = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;

    static int fails;
    static int checks;
    static int cell;

    const long Overflowed = 0x5EED0FF10BADL;

    // ---- reference ----

    [MethodImpl(NoOpt)]
    static long S32(long v)
    {
        v &= 0xFFFFFFFFL;
        if (v >= 0x80000000L) v -= 0x100000000L;
        return v;
    }

    [MethodImpl(NoOpt)] static long Z32(long v) => v & 0xFFFFFFFFL;

    [MethodImpl(NoOpt)]
    static long S16(long v)
    {
        v &= 0xFFFF;
        if (v >= 0x8000) v -= 0x10000;
        return v;
    }

    [MethodImpl(NoOpt)]
    static long Swap(long v, int bytes)
    {
        long r = 0;
        for (int i = 0; i < bytes; i++) r |= ((v >> (8 * i)) & 0xFF) << (8 * (bytes - 1 - i));
        return r;
    }

    [MethodImpl(NoOpt)]
    static long Bit(long index, int width)
    {
        long n = index % width;
        if (n < 0) n += width;
        long b = 1;
        for (long i = 0; i < n; i++) b += b;
        return b;
    }

    [MethodImpl(NoOpt)]
    static long Log2Ref(ulong v)
    {
        long r = 0;
        for (int i = 0; i < 64; i++)
            if (((v >> i) & 1) != 0) r = i;
        return r;
    }

    // ---- byte swaps ----

    [MethodImpl(NI)] static int SwapInt(int x) => BinaryPrimitives.ReverseEndianness(x);
    [MethodImpl(NI)] static uint SwapUInt(uint x) => BinaryPrimitives.ReverseEndianness(x);
    [MethodImpl(NI)] static long SwapLong(long x) => BinaryPrimitives.ReverseEndianness(x);
    [MethodImpl(NI)] static ulong SwapULong(ulong x) => BinaryPrimitives.ReverseEndianness(x);
    [MethodImpl(NI)] static short SwapShort(short x) => BinaryPrimitives.ReverseEndianness(x);
    [MethodImpl(NI)] static ushort SwapUShort(ushort x) => BinaryPrimitives.ReverseEndianness(x);
    [MethodImpl(NI)] static long SwapIntWiden(int x) => (long)BinaryPrimitives.ReverseEndianness(x);
    [MethodImpl(NI)] static long SwapUIntWiden(uint x) => (long)BinaryPrimitives.ReverseEndianness(x);
    [MethodImpl(NI)] static long SwapIntSext(uint x) => (long)(int)BinaryPrimitives.ReverseEndianness(x);
    [MethodImpl(NI)] static bool SwapIntNegative(int x) => BinaryPrimitives.ReverseEndianness(x) < 0;
    [MethodImpl(NI)] static bool SwapIntLess(int x, int y) => BinaryPrimitives.ReverseEndianness(x) < y;
    [MethodImpl(NI)] static bool SwapUIntLess(uint x, uint y) => BinaryPrimitives.ReverseEndianness(x) < y;
    [MethodImpl(NI)] static long SwapIntAdd(int x, long k) => k + BinaryPrimitives.ReverseEndianness(x);
    [MethodImpl(NI)] static long SwapIntOfLong(long x) => BinaryPrimitives.ReverseEndianness((int)x);
    [MethodImpl(NI)] static long SwapIntOfSum(int x, int y) => BinaryPrimitives.ReverseEndianness(x + y);
    [MethodImpl(NI)] static long SwapLongOfInt(int x) => BinaryPrimitives.ReverseEndianness((long)x);
    [MethodImpl(NI)] static long SwapLongOfSum(int x, int y) => BinaryPrimitives.ReverseEndianness((long)(x + y));
    [MethodImpl(NI)] static long SwapULongOfUInt(uint x) => (long)BinaryPrimitives.ReverseEndianness((ulong)x);
    [MethodImpl(NI)] static long SwapTwice(int x) => BinaryPrimitives.ReverseEndianness(BinaryPrimitives.ReverseEndianness(x) + 1);
    [MethodImpl(NI)] static long SwapShortWiden(short x) => (long)BinaryPrimitives.ReverseEndianness(x) + ((long)BinaryPrimitives.ReverseEndianness((ushort)x) << 20);
    [MethodImpl(NI)] static long SwapLoad(int[] a, int i) => BinaryPrimitives.ReverseEndianness(a[i]);
    [MethodImpl(NI)] static long SwapRead(byte* p) => BinaryPrimitives.ReadInt32BigEndian(new ReadOnlySpan<byte>(p, 4)) + ((long)BinaryPrimitives.ReadUInt32BigEndian(new ReadOnlySpan<byte>(p + 4, 4)) << 1);
    [MethodImpl(NI)] static void SwapStore(int[] a, int x) { a[0] = BinaryPrimitives.ReverseEndianness(x); }
    [MethodImpl(NI)] static long SwapIndex(long[] t, int x) => t[BinaryPrimitives.ReverseEndianness(x) & 15] + ((uint)BinaryPrimitives.ReverseEndianness(x) < (uint)t.Length ? 100 : 200);

    // ---- single bits with a variable index ----

    [MethodImpl(NI)] static int Set32(int a, int b) => a | (1 << b);
    [MethodImpl(NI)] static int Flip32(int a, int b) => a ^ (1 << b);
    [MethodImpl(NI)] static int Clear32(int a, int b) => a & ~(1 << b);
    [MethodImpl(NI)] static int Test32(int a, int b) => (a >> b) & 1;
    [MethodImpl(NI)] static bool TestBool32(int a, int b) => (a & (1 << b)) != 0;
    [MethodImpl(NI)] static uint SetU32(uint a, int b) => a | (1u << b);
    [MethodImpl(NI)] static uint FlipU32(uint a, int b) => a ^ (1u << b);
    [MethodImpl(NI)] static uint ClearU32(uint a, int b) => a & ~(1u << b);
    [MethodImpl(NI)] static long Set32Widen(int a, int b) => (long)(a | (1 << b)) + (long)(uint)(a ^ (1 << b)) * 3 + (long)(a & ~(1 << b)) * 5;
    [MethodImpl(NI)] static bool Set32Negative(int a, int b) => (a | (1 << b)) < 0;
    [MethodImpl(NI)] static bool Flip32Less(int a, int b, int c) => (a ^ (1 << b)) < c;
    [MethodImpl(NI)] static long Set64(long a, int b) => a | (1L << b);
    [MethodImpl(NI)] static long Flip64(long a, int b) => a ^ (1L << b);
    [MethodImpl(NI)] static long Clear64(long a, int b) => a & ~(1L << b);
    [MethodImpl(NI)] static long Test64(long a, int b) => (a >> b) & 1;
    [MethodImpl(NI)] static bool TestBool64(long a, int b) => (a & (1L << b)) != 0;
    [MethodImpl(NI)] static long Set64OfInt(int a, int b) => (long)a | (1L << b);
    [MethodImpl(NI)] static long Flip64OfSum(int a, int b, int c) => (long)(a + c) ^ (1L << b);
    [MethodImpl(NI)] static long Clear64OfUInt(uint a, int b) => (long)a & ~(1L << b);
    [MethodImpl(NI)] static long Test64OfSum(int a, int b, int c) => ((long)(a + c) >> b) & 1;
    [MethodImpl(NI)] static long Set64Long(long a, long b) => a | (1L << (int)b);
    // Constant bits: bit 31 of a 32-bit value is its sign.
    [MethodImpl(NI)] static long Const32(int a) => (long)(a | (1 << 31)) + (long)(a ^ (1 << 31)) * 3 + (long)(a & ~(1 << 31)) * 5 + (long)(a | (1 << 30)) * 7 + (long)(a ^ (1 << 11)) * 11 + (long)(a & ~(1 << 12)) * 13;
    [MethodImpl(NI)] static long Const64(long a) => (a | (1L << 31)) + (a ^ (1L << 32)) * 3 + (a & ~(1L << 63)) * 5 + (a | (1L << 63)) * 7 + (a ^ (1L << 11)) * 11 + ((a >> 40) & 1) * 13 + ((a >> 63) & 1) * 17;

    // ---- shift-and-add forms ----

    [MethodImpl(NI)] static long Sh1(long k, long x) => k + (x << 1);
    [MethodImpl(NI)] static long Sh2(long k, long x) => k + x * 4;
    [MethodImpl(NI)] static long Sh3(long k, long x) => (x << 3) + k;
    [MethodImpl(NI)] static long Sh3Int(long k, int x) => k + ((long)x << 3);
    [MethodImpl(NI)] static long Sh2UInt(long k, uint x) => k + ((long)x << 2);
    [MethodImpl(NI)] static long Sh1UIntOfInt(long k, int x) => k + ((long)(uint)x << 1);
    [MethodImpl(NI)] static long AddUInt(long k, uint x) => k + x;
    [MethodImpl(NI)] static long AddUIntOfInt(long k, int x) => (long)(uint)x + k;
    [MethodImpl(NI)] static long AddUIntOfSum(long k, int x, int y) => k + (uint)(x + y);
    [MethodImpl(NI)] static ulong ShlUInt(uint x, int s) => (ulong)x << s;
    [MethodImpl(NI)] static ulong ShlUInt5(uint x) => (ulong)x << 5;
    [MethodImpl(NI)] static ulong ShlUInt32(uint x) => (ulong)x << 32;
    [MethodImpl(NI)] static ulong ShlUInt63(int x) => (ulong)(uint)x << 63;
    [MethodImpl(NI)] static long* Element(long* p, int i) => p + i;
    [MethodImpl(NI)] static long* ElementU(long* p, uint i) => p + i;
    [MethodImpl(NI)] static int* ElementInt(int* p, uint i) => p + i;
    [MethodImpl(NI)] static short* ElementShort(short* p, uint i) => p + i;

    // The 32-bit product or shift is not a 64-bit one.
    [MethodImpl(NI)] static long Sh1Narrow(long k, int x) => k + (long)(x << 1);
    [MethodImpl(NI)] static long Sh2Narrow(long k, int x) => (long)(x * 4) + k;
    [MethodImpl(NI)] static long Sh3Narrow(long k, int x, int y) => k + (long)((x + y) << 3);
    [MethodImpl(NI)] static long Sh2NarrowU(long k, uint x) => k + (long)(x << 2);
    [MethodImpl(NI)] static long Sh3NarrowLoad(long k, int[] a) => k + (long)(a[0] * 8);

    // Checked operations must keep their checks.
    [MethodImpl(NI)] static long CheckedMul(long k, long x) => k + checked(x * 8);
    [MethodImpl(NI)] static long CheckedMul2(long k, long x) => checked(x * 2) + k;
    [MethodImpl(NI)] static long CheckedMulInt(long k, int x) => k + checked(x * 4);
    [MethodImpl(NI)] static long CheckedCast(long k, int x) => k + (long)checked((ulong)x);
    [MethodImpl(NI)] static long CheckedCastShift(long k, int x) => k + ((long)checked((ulong)x) << 2);
    [MethodImpl(NI)] static long CheckedCastUInt(long k, int x) => k + checked((uint)x);
    [MethodImpl(NI)] static long CheckedCastShiftUInt(long k, int x) => k + ((long)checked((uint)x) << 3);
    [MethodImpl(NI)] static ulong CheckedCastShl(int x) => checked((ulong)x) << 5;

    // ---- shift amounts of 64 and more that are only known late ----

    [MethodImpl(NI)] static int Opaque(int v) { cell ^= 0; return v; }

    [MethodImpl(NI)]
    static long Late(uint x, int y, bool f)
    {
        int s64 = f ? 64 : 64;
        int s67 = new int[67].Length;
        long r = (long)((ulong)x << (s64 & 63));
        r = r * 31 + (long)((ulong)x << (s67 & 63));
        r = r * 31 + (long)((ulong)(uint)y << (s67 & 63));
        r = r * 31 + (long)((ulong)(uint)y << ((s64 + 33) & 63));
        return r;
    }

    [MethodImpl(NoOpt)] static long RLate(long x, long y) => ((Z32(x) * 31 + (Z32(x) << 3)) * 31 + (Z32(y) << 3)) * 31 + (Z32(y) << 33);

    // ---- Log2 ----

    [MethodImpl(NI)] static int Log2U(uint x) => BitOperations.Log2(x);
    [MethodImpl(NI)] static int Log2UL(ulong x) => BitOperations.Log2(x);
    [MethodImpl(NI)] static int Log2N(nuint x) => BitOperations.Log2(x);
    [MethodImpl(NI)] static long Log2Widen(uint x, ulong y) => (long)BitOperations.Log2(x) * 100 + BitOperations.Log2(y) + ((long)BitOperations.Log2(x | 1) << 20);
    [MethodImpl(NI)] static long Log2OfInt(int x) => BitOperations.Log2((uint)x) * 100L + BitOperations.Log2((ulong)x) + BitOperations.Log2((ulong)(uint)x) * 10000L;
    [MethodImpl(NI)] static long Log2OfSum(int x, int y) => BitOperations.Log2((ulong)(long)(x + y)) * 100L + BitOperations.Log2((uint)(x + y));
    [MethodImpl(NI)] static long Log2Index(long[] t, ulong x) => t[BitOperations.Log2(x) & 15] + (BitOperations.Log2(x) < 32 ? 3 : 5);
    [MethodImpl(NI)] static bool IsPow2(ulong x) => BitOperations.IsPow2(x);
    [MethodImpl(NI)] static ulong RoundUp(ulong x) => BitOperations.RoundUpToPowerOf2(x);
    [MethodImpl(NI)] static long Counts(ulong x) => BitOperations.LeadingZeroCount(x) + BitOperations.TrailingZeroCount(x) * 100L + BitOperations.PopCount(x) * 10000L
                                                    + BitOperations.LeadingZeroCount((uint)x) * 1000000L + BitOperations.TrailingZeroCount((uint)x) * 100000000L
                                                    + BitOperations.PopCount((uint)x) * 10000000000L;

    [MethodImpl(NoOpt)]
    static long RCounts(ulong x)
    {
        long lz = 0, tz = 0, pop = 0, lz32 = 0, tz32 = 0, pop32 = 0;
        for (int i = 63; i >= 0 && ((x >> i) & 1) == 0; i--) lz++;
        for (int i = 0; i < 64 && ((x >> i) & 1) == 0; i++) tz++;
        for (int i = 0; i < 64; i++) pop += (long)((x >> i) & 1);
        for (int i = 31; i >= 0 && ((x >> i) & 1) == 0; i--) lz32++;
        for (int i = 0; i < 32 && ((x >> i) & 1) == 0; i++) tz32++;
        for (int i = 0; i < 32; i++) pop32 += (long)((x >> i) & 1);
        return lz + tz * 100 + pop * 10000 + lz32 * 1000000 + tz32 * 100000000 + pop32 * 10000000000L;
    }

    // ---- driver ----

    // The driver gets its 32-bit arguments from here, sign-extended whatever the 64-bit value was.
    [MethodImpl(NoOpt)] static int ToInt(long v) => (int)v;
    [MethodImpl(NoOpt)] static uint ToUInt(long v) => (uint)v;


    static readonly long[] Vs =
    {
        0, 1, 2, 3, -1, -2, 0x7F, 0x80, 0xFF, 0x100, 0x7FFF, 0x8000, 0xFFFF, 0x10000, 0x7FFFFFFF, 0x80000000L, 0xFFFFFFFFL,
        0x100000000L, 0x12345678, 0x87654321L, 0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000L, 0x80000080L, 0x00800000,
        0x0123456789ABCDEFL, unchecked((long)0xFEDCBA9876543210), long.MaxValue, long.MinValue, 0x00000080_00000000L,
        unchecked((long)0xFFFFFFFF_00000080), 0x7FFFFFFF_80000000L, 0x55555555_AAAAAAAAL,
    };

    static void Eq(string name, long a, long b, long want, long got)
    {
        checks++;
        if (want == got) return;
        fails++;
        // The first three failures of every check are printed, and a count per check in the end.
        int slot = 0;
        while (slot < failed && failedNames[slot] != name) slot++;
        if (slot == failedNames.Length) return;
        if (slot == failed) failedNames[failed++] = name;
        if (++failedCounts[slot] <= 3)
            Console.WriteLine("FAIL " + name + " (0x" + a.ToString("x") + ", 0x" + b.ToString("x") + ") expected 0x" + want.ToString("x")
                              + " got 0x" + got.ToString("x"));
    }

    static readonly string[] failedNames = new string[128];
    static readonly int[] failedCounts = new int[128];
    static int failed;

    [MethodImpl(NoOpt)] static long B(bool f) => f ? 1 : 0;

    static long Catch1(delegate*<long, long, long> f, long k, long x)
    {
        try { return f(k, x); }
        catch (OverflowException) { return Overflowed; }
    }

    static long Catch2(delegate*<long, int, long> f, long k, int x)
    {
        try { return f(k, x); }
        catch (OverflowException) { return Overflowed; }
    }

    static long Catch3(int x)
    {
        try { return (long)CheckedCastShl(x); }
        catch (OverflowException) { return Overflowed; }
    }

    [MethodImpl(NoOpt)] static bool Fits(long v, long scale) => v <= long.MaxValue / scale && v >= long.MinValue / scale;

    static void Run()
    {
        long[] tab = new long[16];
        for (int i = 0; i < 16; i++) tab[i] = (i + 1) * 0x0101010101010101L;
        int[] one = new int[2];
        byte* raw = stackalloc byte[8];
        long* mem = stackalloc long[2];

        foreach (long v in Vs)
        {
            int x = ToInt(v);
            uint u = ToUInt(v);
            long sx = S32(v), zx = Z32(v);
            long swapped32 = Swap(v, 4);

            Eq("swap int", v, 0, S32(swapped32), SwapInt(x));
            Eq("swap uint", v, 0, swapped32, SwapUInt(u));
            Eq("swap long", v, 0, Swap(v, 8), SwapLong(v));
            Eq("swap ulong", v, 0, Swap(v, 8), (long)SwapULong((ulong)v));
            Eq("swap short", v, 0, S16(Swap(v, 2)), SwapShort((short)x));
            Eq("swap ushort", v, 0, Swap(v, 2), SwapUShort((ushort)x));
            Eq("swap int, widened", v, 0, S32(swapped32), SwapIntWiden(x));
            Eq("swap uint, widened", v, 0, swapped32, SwapUIntWiden(u));
            Eq("swap uint, sign-extended", v, 0, S32(swapped32), SwapIntSext(u));
            Eq("swap int < 0", v, 0, B(S32(swapped32) < 0), B(SwapIntNegative(x)));
            Eq("k + swap int", v, 0, 0x100000000L + S32(swapped32), SwapIntAdd(x, 0x100000000L));
            Eq("swap (int)long", v, 0, S32(swapped32), SwapIntOfLong(v));
            Eq("swap (long)int", v, 0, Swap(sx, 8), SwapLongOfInt(x));
            Eq("swap (ulong)uint", v, 0, Swap(zx, 8), SwapULongOfUInt(u));
            Eq("swap twice", v, 0, S32(Swap(S32(swapped32) + 1, 4)), SwapTwice(x));
            Eq("swap shorts, widened", v, 0, S16(Swap(v, 2)) + (Swap(v, 2) << 20), SwapShortWiden((short)x));
            one[0] = x;
            Eq("swap loaded int", v, 0, S32(swapped32), SwapLoad(one, 0));
            SwapStore(one, x);
            Eq("swap stored int", v, 0, S32(swapped32), one[0]);
            Eq("swap as index", v, 0, tab[swapped32 & 15] + (swapped32 < 16 ? 100 : 200), SwapIndex(tab, x));
            for (int i = 0; i < 8; i++) raw[i] = (byte)(v >> (8 * i));
            Eq("big-endian reads", v, 0, S32(swapped32) + (Swap(v >> 32, 4) << 1), SwapRead(raw));

            Eq("Log2(uint)", v, 0, Log2Ref((ulong)zx), Log2U(u));
            Eq("Log2(ulong)", v, 0, Log2Ref((ulong)v), Log2UL((ulong)v));
            Eq("Log2(nuint)", v, 0, Log2Ref((ulong)(nuint)(ulong)v), Log2N((nuint)(ulong)v));
            Eq("Log2 widened", v, 0, Log2Ref((ulong)zx) * 100 + Log2Ref((ulong)v) + (Log2Ref((ulong)(zx | 1)) << 20), Log2Widen(u, (ulong)v));
            Eq("Log2 of an int", v, 0, Log2Ref((ulong)zx) * 100 + Log2Ref((ulong)sx) + Log2Ref((ulong)zx) * 10000, Log2OfInt(x));
            Eq("Log2 as index", v, 0, tab[Log2Ref((ulong)v) & 15] + (Log2Ref((ulong)v) < 32 ? 3 : 5), Log2Index(tab, (ulong)v));
            Eq("bit counts", v, 0, RCounts((ulong)v), Counts((ulong)v));
            Eq("32-bit constant bits", v, 0, (sx | -0x80000000L) + S32(sx ^ 0x80000000L) * 3 + (sx & 0x7FFFFFFFL) * 5 + (sx | 0x40000000) * 7 + (sx ^ 0x800) * 11 + (sx & ~0x1000L) * 13, Const32(x));
            Eq("64-bit constant bits", v, 0, (v | 0x80000000L) + (v ^ 0x100000000L) * 3 + (v & long.MaxValue) * 5 + (v | long.MinValue) * 7 + (v ^ 0x800) * 11 + ((v >> 40) & 1) * 13 + ((v >> 63) & 1) * 17, Const64(v));

            Eq("k + (x << 1)", v, 0, 77 + v * 2, Sh1(77, v));
            Eq("k + x * 4", v, 0, 77 + v * 4, Sh2(77, v));
            Eq("(x << 3) + k", v, 0, 77 + v * 8, Sh3(77, v));
            Eq("k + ((long)int << 3)", v, 0, 77 + sx * 8, Sh3Int(77, x));
            Eq("k + ((long)uint << 2)", v, 0, 77 + zx * 4, Sh2UInt(77, u));
            Eq("k + ((long)(uint)int << 1)", v, 0, 77 + zx * 2, Sh1UIntOfInt(77, x));
            Eq("k + uint", v, 0, 77 + zx, AddUInt(77, u));
            Eq("(uint)int + k", v, 0, 77 + zx, AddUIntOfInt(77, x));
            Eq("(ulong)uint << 5", v, 0, zx << 5, (long)ShlUInt5(u));
            Eq("(ulong)uint << 32", v, 0, zx << 32, (long)ShlUInt32(u));
            Eq("(ulong)(uint)int << 63", v, 0, zx << 63, (long)ShlUInt63(x));
            Eq("p + int", v, 0, (long)(nint)(sx * 8), (long)((nint)Element(mem, x) - (nint)mem));
            Eq("p + uint", v, 0, (long)(nint)(zx * 8), (long)((nint)ElementU(mem, u) - (nint)mem));
            Eq("int* + uint", v, 0, (long)(nint)(zx * 4), (long)((nint)ElementInt((int*)mem, u) - (nint)mem));
            Eq("short* + uint", v, 0, (long)(nint)(zx * 2), (long)((nint)ElementShort((short*)mem, u) - (nint)mem));
            Eq("k + (long)(int << 1)", v, 0, 77 + S32(sx * 2), Sh1Narrow(77, x));
            Eq("(long)(int * 4) + k", v, 0, 77 + S32(sx * 4), Sh2Narrow(77, x));
            Eq("k + (long)(uint << 2)", v, 0, 77 + Z32(zx * 4), Sh2NarrowU(77, u));
            one[0] = x;
            Eq("k + (long)(load * 8)", v, 0, 77 + S32(sx * 8), Sh3NarrowLoad(77, one));
            Eq("late shift amounts", v, 0, RLate(v, ~v), Late(u, ~x, Opaque(1) == 1));

            Eq("k + checked(x * 8)", v, 0, Fits(v, 8) ? 77 + v * 8 : Overflowed, Catch1(&CheckedMul, 77, v));
            Eq("checked(x * 2) + k", v, 0, Fits(v, 2) ? 77 + v * 2 : Overflowed, Catch1(&CheckedMul2, 77, v));
            Eq("k + checked(int * 4)", v, 0, sx * 4 == S32(sx * 4) ? 77 + sx * 4 : Overflowed, Catch2(&CheckedMulInt, 77, x));
            Eq("k + checked((ulong)int)", v, 0, sx >= 0 ? 77 + sx : Overflowed, Catch2(&CheckedCast, 77, x));
            Eq("k + (checked((ulong)int) << 2)", v, 0, sx >= 0 ? 77 + sx * 4 : Overflowed, Catch2(&CheckedCastShift, 77, x));
            Eq("k + checked((uint)int)", v, 0, sx >= 0 ? 77 + sx : Overflowed, Catch2(&CheckedCastUInt, 77, x));
            Eq("k + (checked((uint)int) << 3)", v, 0, sx >= 0 ? 77 + sx * 8 : Overflowed, Catch2(&CheckedCastShiftUInt, 77, x));
            Eq("checked((ulong)int) << 5", v, 0, sx >= 0 ? sx << 5 : Overflowed, Catch3(x));

            foreach (long w in Vs)
            {
                int y = ToInt(w);
                Eq("swap int < int", v, w, B(S32(swapped32) < S32(w)), B(SwapIntLess(x, y)));
                Eq("swap uint < uint", v, w, B(swapped32 < Z32(w)), B(SwapUIntLess(u, ToUInt(w))));
                Eq("swap (int + int)", v, w, S32(Swap(sx + S32(w), 4)), SwapIntOfSum(x, y));
                Eq("swap (long)(int + int)", v, w, Swap(S32(sx + S32(w)), 8), SwapLongOfSum(x, y));
                Eq("k + (uint)(int + int)", v, w, 77 + Z32(sx + S32(w)), AddUIntOfSum(77, x, y));
                Eq("k + (long)((int + int) << 3)", v, w, 77 + S32((sx + S32(w)) * 8), Sh3Narrow(77, x, y));
                Eq("Log2 of a sum", v, w, Log2Ref((ulong)S32(sx + S32(w))) * 100 + Log2Ref((ulong)Z32(sx + S32(w))), Log2OfSum(x, y));
            }

            for (int b = -70; b <= 70; b++)
            {
                long bit32 = S32(Bit(b, 32)), bit64 = Bit(b, 64);
                long shift32 = b & 31, shift64 = b & 63;
                Eq("a | (1 << b)", v, b, sx | bit32, Set32(x, b));
                Eq("a ^ (1 << b)", v, b, sx ^ bit32, Flip32(x, b));
                Eq("a & ~(1 << b)", v, b, sx & ~bit32, Clear32(x, b));
                Eq("(a >> b) & 1", v, b, (sx >> (int)shift32) & 1, Test32(x, b));
                Eq("(a & (1 << b)) != 0", v, b, B((sx & bit32) != 0), B(TestBool32(x, b)));
                Eq("uint | (1u << b)", v, b, Z32(zx | bit32), SetU32(u, b));
                Eq("uint ^ (1u << b)", v, b, Z32(zx ^ bit32), FlipU32(u, b));
                Eq("uint & ~(1u << b)", v, b, Z32(zx & ~bit32), ClearU32(u, b));
                Eq("32-bit bits, widened", v, b, (sx | bit32) + Z32(sx ^ bit32) * 3 + (sx & ~bit32) * 5, Set32Widen(x, b));
                Eq("(a | (1 << b)) < 0", v, b, B((sx | bit32) < 0), B(Set32Negative(x, b)));
                Eq("(a ^ (1 << b)) < c", v, b, B((sx ^ bit32) < -5), B(Flip32Less(x, b, -5)));
                Eq("long | (1L << b)", v, b, v | bit64, Set64(v, b));
                Eq("long ^ (1L << b)", v, b, v ^ bit64, Flip64(v, b));
                Eq("long & ~(1L << b)", v, b, v & ~bit64, Clear64(v, b));
                Eq("(long >> b) & 1", v, b, (v >> (int)shift64) & 1, Test64(v, b));
                Eq("(long & (1L << b)) != 0", v, b, B((v & bit64) != 0), B(TestBool64(v, b)));
                Eq("(long)int | (1L << b)", v, b, sx | bit64, Set64OfInt(x, b));
                Eq("(long)(int + int) ^ (1L << b)", v, b, S32(sx + 0x7FFFFFFF) ^ bit64, Flip64OfSum(x, b, int.MaxValue));
                Eq("(long)uint & ~(1L << b)", v, b, zx & ~bit64, Clear64OfUInt(u, b));
                Eq("((long)(int + int) >> b) & 1", v, b, (S32(sx + 0x7FFFFFFF) >> (int)shift64) & 1, Test64OfSum(x, b, int.MaxValue));
                Eq("long | (1L << (int)long)", v, b, v | bit64, Set64Long(v, b + 0x300000000L));
                Eq("(ulong)uint << b", v, b, zx << (int)shift64, (long)ShlUInt(u, b));
            }
        }

        for (int i = 0; i < 64; i++)
        {
            ulong p = 1UL << i;
            Eq("Log2(2^i)", i, 0, i, Log2UL(p));
            Eq("Log2(2^i - 1)", i, 0, i == 0 ? 0 : i - 1, Log2UL(p - 1));
            Eq("Log2(2^i + 1)", i, 0, i == 0 ? 1 : i, Log2UL(p + 1));
            Eq("IsPow2(2^i)", i, 0, 1, B(IsPow2(p)));
            Eq("IsPow2(2^i + 1)", i, 0, i == 0 ? 1 : 0, B(IsPow2(p + 1)));
            Eq("RoundUp(2^i)", i, 0, (long)p, (long)RoundUp(p));
            if (i >= 2 && i < 63) Eq("RoundUp(2^i + 1)", i, 0, (long)(p << 1), (long)RoundUp(p + 1));
            if (i < 32)
            {
                Eq("Log2((uint)2^i)", i, 0, i, Log2U((uint)p));
                Eq("Log2((uint)(2^i - 1))", i, 0, i == 0 ? 0 : i - 1, Log2U((uint)p - 1));
            }
        }
    }

    static int Main()
    {
        Run();
        for (int i = 0; i < failed; i++) Console.WriteLine("FAIL " + failedNames[i] + ": " + failedCounts[i] + " checks");
        Console.WriteLine(fails == 0 ? "ALL PASS (" + checks + " checks)" : "FAILED " + fails + " of " + checks);
        return fails == 0 ? 0 : 1;
    }
}
