// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Shift counts and the masks around them.
//
// sll, srl and sra read the low 6 bits of the count and their W forms the
// low 5, which is what a C# shift means. The compiler therefore drops an
// AND of the count with a constant that keeps all of those bits (& 63 on a
// 64-bit shift, & 31, & 63 or & 255 on a 32-bit one) before the count can
// be shared between several shifts, and drops an added or subtracted
// multiple of the width ((64 - s) becomes -s). Constant folding of a 32-bit
// shift masks the count with 31. What can go wrong: a mask that does not
// keep all the count bits is dropped (& 31 on a 64-bit shift, & 15), a
// constant that is not a multiple of the width is dropped (32 on a 64-bit
// shift), the mask is dropped where the count has another reader, a count
// that turns out constant and is 32 or more reaches code that does not
// mask it, and a shift that is not done by one of those instructions (a
// rotate, a shift-and-add, a bit test) gets an unmasked count.
//
// Every shift here is checked for counts from -140 to 140 against a
// reference that shifts one bit at a time and never uses a variable shift.
//
// A failed check prints one line; the run ends with ALL PASS or a FAIL
// count, and the guest returns 1 when anything failed.
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

unsafe class Program
{
    const MethodImplOptions NI = MethodImplOptions.NoInlining;
    const MethodImplOptions NoOpt = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;

    static int fails;
    static int checks;
    static int shown;
    static int cell;
    static int mask63 = 63;
    static int mask31 = 31;

    // ---- reference: one bit at a time ----

    [MethodImpl(NoOpt)]
    static int Norm(long count, int width)
    {
        long m = count % width;
        if (m < 0) m += width;
        return (int)m;
    }

    [MethodImpl(NoOpt)]
    static long Shl64(long x, long count)
    {
        int n = Norm(count, 64);
        for (int i = 0; i < n; i++) x += x;
        return x;
    }

    [MethodImpl(NoOpt)]
    static long Sar64(long x, long count)
    {
        int n = Norm(count, 64);
        for (int i = 0; i < n; i++) x = (x - (x & 1)) / 2;
        return x;
    }

    [MethodImpl(NoOpt)]
    static long Shr64(long x, long count)
    {
        int n = Norm(count, 64);
        for (int i = 0; i < n; i++)
        {
            long top = x < 0 ? 0x4000000000000000L : 0;
            x = ((x & 0x7FFFFFFFFFFFFFFFL) - (x & 1)) / 2 + top;
        }
        return x;
    }

    [MethodImpl(NoOpt)]
    static long S32(long v)
    {
        v &= 0xFFFFFFFFL;
        if (v >= 0x80000000L) v -= 0x100000000L;
        return v;
    }

    [MethodImpl(NoOpt)] static long Z32(long v) => v & 0xFFFFFFFFL;
    [MethodImpl(NoOpt)] static long Shl32(long x, long count) => S32(Shl64(x, Norm(count, 32)));
    [MethodImpl(NoOpt)] static long Sar32(long x, long count) => Sar64(S32(x), Norm(count, 32));
    [MethodImpl(NoOpt)] static long Shr32(long x, long count) => S32(Shr64(Z32(x), Norm(count, 32)));
    [MethodImpl(NoOpt)] static long Rol64(long x, long count) => Shl64(x, count) | (Norm(count, 64) == 0 ? 0 : Shr64(x, 64 - Norm(count, 64)));
    [MethodImpl(NoOpt)] static long Rol32(long x, long count) => S32(Shl64(Z32(x), Norm(count, 32)) | Shr64(Z32(x), 32 - Norm(count, 32)));

    // ---- 64-bit shifts ----

    [MethodImpl(NI)] static long L0(long x, int s) => x << s;
    [MethodImpl(NI)] static long L1(long x, int s) => x << (s & 63);
    [MethodImpl(NI)] static long L2(long x, int s) => x << (s & 127);
    [MethodImpl(NI)] static long L3(long x, int s) => x << (s & 0x7FFFFFFF);
    [MethodImpl(NI)] static long L4(long x, int s) => x << (s & -1);
    [MethodImpl(NI)] static long L5(long x, int s) => x << ((s & 63) & 255);
    [MethodImpl(NI)] static long L6(long x, int s) => x << (s + 64);
    [MethodImpl(NI)] static long L7(long x, int s) => x << (s - 64);
    [MethodImpl(NI)] static long L8(long x, int s) => x << (s + 128);
    [MethodImpl(NI)] static long L9(long x, int s) => x << ((s + 64) & 63);
    [MethodImpl(NI)] static long L10(long x, int s) => x << (64 - s);
    [MethodImpl(NI)] static long L11(long x, int s) => x << ((64 - s) & 63);
    [MethodImpl(NI)] static long L12(long x, int s) => x << (-s);
    [MethodImpl(NI)] static long L13(long x, int s) => x << (s + 64 + 64 - 128);
    [MethodImpl(NI)] static long L14(long x, int s) => x << (int)((long)s & 63);
    [MethodImpl(NI)] static long L15(long x, long s) => x << (int)(s & 63);
    [MethodImpl(NI)] static long L16(long x, long s) => x << (int)s;
    [MethodImpl(NI)] static long L17(long x, int s) => x << (s & mask63);
    [MethodImpl(NI)] static long L18(long x, byte s) => x << s;
    [MethodImpl(NI)] static long L19(long x, sbyte s) => x << s;
    [MethodImpl(NI)] static long L20(long x, int s) => x << (s | 64);
    [MethodImpl(NI)] static long L21(long x, int s) => x << (s ^ 64);
    [MethodImpl(NI)] static long L22(long x, int s) => x << (int)((uint)s & 63u);

    // Masks and constants that change the count: they must stay.
    [MethodImpl(NI)] static long M0(long x, int s) => x << (s & 31);
    [MethodImpl(NI)] static long M1(long x, int s) => x << (s & 15);
    [MethodImpl(NI)] static long M2(long x, int s) => x << (s & 62);
    [MethodImpl(NI)] static long M3(long x, int s) => x << (s & 64);
    [MethodImpl(NI)] static long M4(long x, int s) => x << (s + 32);
    [MethodImpl(NI)] static long M5(long x, int s) => x << (32 - s);
    [MethodImpl(NI)] static long M6(long x, int s) => x << (s + 63);
    [MethodImpl(NI)] static long M7(long x, int s) => x << ((s & 31) + 32);
    [MethodImpl(NI)] static long M8(long x, int s) => x << ((s + 32) & 63);
    [MethodImpl(NI)] static long M9(long x, int s) => x << (s & 0x3E);
    [MethodImpl(NI)] static long M10(long x, int s) => x << (s & mask31);
    [MethodImpl(NI)] static long M11(long x, int s) => x << ((s & 63) >> 1);
    [MethodImpl(NI)] static long M12(long x, int s) => x << (s * 2);
    [MethodImpl(NI)] static long M13(long x, int s) => x << (s + 1);
    [MethodImpl(NI)] static long M14(long x, int s) => x << (s - 1);
    [MethodImpl(NI)] static long M15(long x, int s) => x << (63 - s);
    [MethodImpl(NI)] static long M16(long x, int s) => x << ~s;

    [MethodImpl(NI)] static long R0(long x, int s) => x >> s;
    [MethodImpl(NI)] static long R1(long x, int s) => x >> (s & 63);
    [MethodImpl(NI)] static long R2(long x, int s) => x >> (64 - s);
    [MethodImpl(NI)] static long R3(long x, int s) => x >> ((64 - s) & 63);
    [MethodImpl(NI)] static long R4(long x, int s) => x >> (s & 31);
    [MethodImpl(NI)] static long R5(long x, int s) => x >> (s + 32);
    [MethodImpl(NI)] static long R6(long x, int s) => x >> (s + 64);
    [MethodImpl(NI)] static long R7(long x, int s) => x >> (s & 255);
    [MethodImpl(NI)] static ulong U0(ulong x, int s) => x >> s;
    [MethodImpl(NI)] static ulong U1(ulong x, int s) => x >> (s & 63);
    [MethodImpl(NI)] static ulong U2(ulong x, int s) => x >> (64 - s);
    [MethodImpl(NI)] static ulong U3(ulong x, int s) => x >> ((64 - s) & 63);
    [MethodImpl(NI)] static ulong U4(ulong x, int s) => x >> (s & 31);
    [MethodImpl(NI)] static ulong U5(ulong x, int s) => x >> (s - 64);
    [MethodImpl(NI)] static ulong U6(ulong x, int s) => x >> (32 - s);
    [MethodImpl(NI)] static ulong U7(ulong x, int s) => x >> ((s & 63) & 63);

    // ---- 32-bit shifts ----

    [MethodImpl(NI)] static int I0(int x, int s) => x << s;
    [MethodImpl(NI)] static int I1(int x, int s) => x << (s & 31);
    [MethodImpl(NI)] static int I2(int x, int s) => x << (s & 63);
    [MethodImpl(NI)] static int I3(int x, int s) => x << (s & 255);
    [MethodImpl(NI)] static int I4(int x, int s) => x << (s + 32);
    [MethodImpl(NI)] static int I5(int x, int s) => x << (s - 32);
    [MethodImpl(NI)] static int I6(int x, int s) => x << (s + 64);
    [MethodImpl(NI)] static int I7(int x, int s) => x << (32 - s);
    [MethodImpl(NI)] static int I8(int x, int s) => x << ((32 - s) & 31);
    [MethodImpl(NI)] static int I9(int x, int s) => x << (64 - s);
    [MethodImpl(NI)] static int I10(int x, int s) => x << (-s);
    [MethodImpl(NI)] static int I11(int x, long s) => x << (int)(s & 31);
    [MethodImpl(NI)] static int I12(int x, int s) => x << (s & mask31);
    [MethodImpl(NI)] static int I13(int x, int s) => x << (s & 15);
    [MethodImpl(NI)] static int I14(int x, int s) => x << (s & 30);
    [MethodImpl(NI)] static int I15(int x, int s) => x << (s + 16);
    [MethodImpl(NI)] static int I16(int x, int s) => x << (16 - s);
    [MethodImpl(NI)] static int I17(int x, int s) => x << (s + 31);
    [MethodImpl(NI)] static int I18(int x, int s) => x << (s & 32);
    [MethodImpl(NI)] static int I19(int x, int s) => x << ((s & 15) + 16);
    [MethodImpl(NI)] static int IR0(int x, int s) => x >> s;
    [MethodImpl(NI)] static int IR1(int x, int s) => x >> (s & 31);
    [MethodImpl(NI)] static int IR2(int x, int s) => x >> (s & 63);
    [MethodImpl(NI)] static int IR3(int x, int s) => x >> (32 - s);
    [MethodImpl(NI)] static int IR4(int x, int s) => x >> (s + 32);
    [MethodImpl(NI)] static int IR5(int x, int s) => x >> (s & 15);
    [MethodImpl(NI)] static int IR6(int x, int s) => x >> (s + 16);
    [MethodImpl(NI)] static uint IU0(uint x, int s) => x >> s;
    [MethodImpl(NI)] static uint IU1(uint x, int s) => x >> (s & 31);
    [MethodImpl(NI)] static uint IU2(uint x, int s) => x >> (s & 63);
    [MethodImpl(NI)] static uint IU3(uint x, int s) => x >> (32 - s);
    [MethodImpl(NI)] static uint IU4(uint x, int s) => x >> ((32 - s) & 31);
    [MethodImpl(NI)] static uint IU5(uint x, int s) => x >> (s - 32);
    [MethodImpl(NI)] static uint IU6(uint x, int s) => x >> (s & 15);
    [MethodImpl(NI)] static uint IU7(uint x, int s) => x >> (s + 16);

    // The result of a 32-bit shift widened: the shift must stay a 32-bit one.
    [MethodImpl(NI)] static long W0(int x, int s) => (long)(x << (s & 31));
    [MethodImpl(NI)] static long W1(int x, int s) => (long)(x >> (s & 31)) + (long)(uint)(x << s);
    [MethodImpl(NI)] static ulong W2(uint x, int s) => (ulong)(x >> (s & 31)) + (ulong)(x << (s & 63));
    [MethodImpl(NI)] static long W3(int x, int s, long k) => k + (long)(x << (s & 31));

    // ---- a count shared by several shifts ----

    [MethodImpl(NI)]
    static long Shared64(long a, long b, long c, int s)
        => (a << (s & 63)) ^ (b >> (s & 63)) ^ (long)((ulong)c >> (s & 63)) ^ (a >> ((64 - s) & 63)) ^ (b << ((64 - s) & 63));

    [MethodImpl(NoOpt)]
    static long RShared64(long a, long b, long c, long s)
        => Shl64(a, s) ^ Sar64(b, s) ^ Shr64(c, s) ^ Sar64(a, 64 - s) ^ Shl64(b, 64 - s);

    [MethodImpl(NI)]
    static long Shared32(int a, int b, int c, int s)
    {
        int n = s & 31;
        return (long)(a << n) ^ ((long)(b >> n) << 8) ^ ((long)(uint)((uint)c >> n) << 16) ^ ((long)(a >> (32 - n)) << 24) ^ n;
    }

    [MethodImpl(NoOpt)]
    static long RShared32(long a, long b, long c, long s)
    {
        long n = Norm(s, 32);
        return Shl32(a, n) ^ Shl64(Sar32(b, n), 8) ^ Shl64(Z32(Shr32(c, n)), 16) ^ Shl64(Sar32(a, 32 - n), 24) ^ n;
    }

    // The masked count is also a value.
    [MethodImpl(NI)]
    static long CountUsed(long a, int s)
    {
        int n = s & 63;
        return (a << n) + n * 1000003L + ((a >> n) ^ Tab[n & 15]);
    }

    [MethodImpl(NoOpt)] static long RCountUsed(long a, long s) => Shl64(a, s) + Norm(s, 64) * 1000003L + (Sar64(a, s) ^ Tab[Norm(s, 64) & 15]);

    [MethodImpl(NI)]
    static long CountUsed32(int a, int s)
    {
        int n = s & 63;         // more bits than a 32-bit shift reads
        return (long)(a << n) * 3 + n + ((long)a << n);
    }

    [MethodImpl(NoOpt)] static long RCountUsed32(long a, long s) => Shl32(a, Norm(s, 64)) * 3 + Norm(s, 64) + Shl64(a, s);

    [MethodImpl(NI)]
    static long InLoop(long[] v, int s)
    {
        long r = 0;
        for (int i = 0; i < v.Length; i++)
            r = (r << (s & 63)) ^ (v[i] >> ((64 - s) & 63)) ^ (long)((ulong)r >> ((s + i) & 63));
        return r;
    }

    [MethodImpl(NoOpt)]
    static long RInLoop(long[] v, long s)
    {
        long r = 0;
        for (int i = 0; i < v.Length; i++)
            r = Shl64(r, s) ^ Sar64(v[i], 64 - s) ^ Shr64(r, s + i);
        return r;
    }

    // ---- rotates and other users of a count ----

    [MethodImpl(NI)] static ulong Rot0(ulong x, int s) => (x << (s & 63)) | (x >> ((64 - s) & 63));
    [MethodImpl(NI)] static ulong Rot1(ulong x, int s) => (x << s) | (x >> (64 - s));
    [MethodImpl(NI)] static ulong Rot2(ulong x, int s) => BitOperations.RotateLeft(x, s);
    [MethodImpl(NI)] static ulong Rot3(ulong x, int s) => BitOperations.RotateLeft(x, s & 63);
    [MethodImpl(NI)] static ulong Rot4(ulong x, int s) => BitOperations.RotateRight(x, 64 - s);
    [MethodImpl(NI)] static ulong Rot5(ulong x, int s) => BitOperations.RotateLeft(x, s + 64);
    [MethodImpl(NI)] static ulong Rot6(ulong x, int s) => (x >> (-s & 63)) | (x << (s & 63));
    [MethodImpl(NI)] static uint Rot32a(uint x, int s) => (x << (s & 31)) | (x >> ((32 - s) & 31));
    [MethodImpl(NI)] static uint Rot32b(uint x, int s) => (x << s) | (x >> (32 - s));
    [MethodImpl(NI)] static uint Rot32c(uint x, int s) => BitOperations.RotateLeft(x, s);
    [MethodImpl(NI)] static uint Rot32d(uint x, int s) => BitOperations.RotateLeft(x, s & 63);
    [MethodImpl(NI)] static uint Rot32e(uint x, int s) => BitOperations.RotateRight(x, 32 - s);
    [MethodImpl(NI)] static uint Rot32f(uint x, int s) => BitOperations.RotateLeft(x, s + 32);
    [MethodImpl(NI)] static uint Rot32g(uint x, int s) => BitOperations.RotateLeft(x, s + 64);

    [MethodImpl(NI)] static long Bit0(long x, int s) => (x >> (s & 63)) & 1;
    [MethodImpl(NI)] static long Bit1(long x, int s) => x | (1L << (s & 63));
    [MethodImpl(NI)] static long Bit2(long x, int s) => x & ~(1L << (s & 63));
    [MethodImpl(NI)] static long Bit3(long x, int s) => x ^ (1L << (s + 64));
    [MethodImpl(NI)] static int Bit4(int x, int s) => (x >> (s & 31)) & 1;
    [MethodImpl(NI)] static int Bit5(int x, int s) => x | (1 << (s & 31));
    [MethodImpl(NI)] static int Bit6(int x, int s) => x & ~(1 << (s & 63));
    [MethodImpl(NI)] static int Bit7(int x, int s) => x ^ (1 << (s + 32));
    [MethodImpl(NI)] static bool Bit8(long x, int s) => (x & (1L << (s & 63))) != 0;
    [MethodImpl(NI)] static bool Bit9(int x, int s) => (x & (1 << (s & 31))) != 0;
    [MethodImpl(NI)] static long ShAdd0(long k, long x, int s) => k + (x << (s & 63));
    [MethodImpl(NI)] static long ShAdd1(long k, int x, int s) => k + ((long)x << (s & 63));
    [MethodImpl(NI)] static long ShAdd2(long k, uint x, int s) => k + ((long)x << (s & 63));

    // ---- counts that turn out constant after the mask was dropped ----

    static long[] Tab;

    [MethodImpl(NI)] static int Opaque(int v) { cell ^= 0; return v; }

    [MethodImpl(NI)]
    static long Late32(int x, uint u, bool f)
    {
        int s32 = f ? 32 : 32;      // a constant only value numbering sees
        int s33 = new int[33].Length;
        int s35 = f ? 35 : 35;
        long r = (long)(x << (s32 & 31));
        r = r * 31 + (long)(x << (s33 & 31));
        r = r * 31 + (long)(x >> (s32 & 31));
        r = r * 31 + (long)(x >> (s35 & 31));
        r = r * 31 + (long)(u >> (s32 & 31));
        r = r * 31 + (long)(u >> (s33 & 31));
        r = r * 31 + (long)(uint)(x << (s35 & 31));
        r = r * 31 + ((u >> (s32 & 31)) > 0x7FFFFFFFu ? 3 : 5);
        r = r * 31 + ((x << (s32 & 31)) < 0 ? 7 : 11);
        return r;
    }

    [MethodImpl(NoOpt)]
    static long RLate32(long x, long u)
    {
        long r = S32(x);
        r = r * 31 + Shl32(x, 1);
        r = r * 31 + S32(x);
        r = r * 31 + Sar32(x, 3);
        r = r * 31 + Z32(u);
        r = r * 31 + Z32(Shr32(u, 1));
        r = r * 31 + Z32(Shl32(x, 3));
        r = r * 31 + (Z32(u) > 0x7FFFFFFFL ? 3 : 5);
        r = r * 31 + (S32(x) < 0 ? 7 : 11);
        return r;
    }

    [MethodImpl(NI)]
    static long Late64(long x, ulong u, bool f)
    {
        int s64 = f ? 64 : 64;
        int s65 = new int[65].Length;
        int s70 = f ? 70 : 70;
        long r = x << (s64 & 63);
        r = r * 31 + (x << (s65 & 63));
        r = r * 31 + (x >> (s64 & 63));
        r = r * 31 + (x >> (s70 & 63));
        r = r * 31 + (long)(u >> (s64 & 63));
        r = r * 31 + (long)(u >> (s65 & 63));
        r = r * 31 + ((u >> (s64 & 63)) > 0x7FFFFFFFFFFFFFFFUL ? 3 : 5);
        r = r * 31 + (int)(x >> (s64 & 63));
        r = r * 31 + (int)(u >> ((s64 + 33) & 63));
        return r;
    }

    [MethodImpl(NoOpt)]
    static long RLate64(long x, long u)
    {
        long r = x;
        r = r * 31 + Shl64(x, 1);
        r = r * 31 + x;
        r = r * 31 + Sar64(x, 6);
        r = r * 31 + u;
        r = r * 31 + Shr64(u, 1);
        r = r * 31 + (u < 0 ? 3 : 5);
        r = r * 31 + S32(x);
        r = r * 31 + S32(Shr64(u, 33));
        return r;
    }

    // The same with the count stored to a static field and read back.
    static int field;

    [MethodImpl(NI)]
    static long LateField(int x, long y)
    {
        field = 37;
        int s = field;
        long r = (long)(x << (s & 31)) * 3 + (long)((uint)x >> (s & 31));
        field = 67;
        s = field;
        return r * 31 + (y << (s & 63)) + (long)((ulong)y >> (s & 63));
    }

    [MethodImpl(NoOpt)] static long RLateField(long x, long y) => (Shl32(x, 5) * 3 + Z32(Shr32(x, 5))) * 31 + Shl64(y, 3) + Shr64(y, 3);

    // ---- driver ----

    // The driver gets its 32-bit arguments from here, sign-extended whatever the 64-bit value was.
    [MethodImpl(NoOpt)] static int ToInt(long v) => (int)v;
    [MethodImpl(NoOpt)] static uint ToUInt(long v) => (uint)v;


    static readonly long[] Xs =
    {
        1, -1, 0x7FFFFFFFFFFFFFFFL, long.MinValue, 0x0123456789ABCDEFL, unchecked((long)0xFEDCBA9876543210),
        0x00000000FFFFFFFFL, unchecked((long)0xFFFFFFFF00000000), 0x80000000L, 0x40000000L, 0x5555555555555555L,
    };

    static void Eq(string name, long x, long s, long want, long got)
    {
        checks++;
        if (want == got) return;
        fails++;
        if (++shown <= 60)
            Console.WriteLine("FAIL " + name + " x=0x" + x.ToString("x") + " s=" + s + " expected 0x" + want.ToString("x") + " got 0x" + got.ToString("x"));
    }

    [MethodImpl(NoOpt)] static long B(bool f) => f ? 1 : 0;

    static void Run()
    {
        long[] vec = { 0x0123456789ABCDEFL, -1, 0x7FFFFFFF80000001L, long.MinValue };
        foreach (long x in Xs)
        {
            int xi = ToInt(x);
            ulong ux = (ulong)x;
            uint ui = ToUInt(x);
            long sx = S32(x);
            for (int s = -140; s <= 140; s++)
            {
                Eq("x << s", x, s, Shl64(x, s), L0(x, s));
                Eq("x << (s & 63)", x, s, Shl64(x, s), L1(x, s));
                Eq("x << (s & 127)", x, s, Shl64(x, s), L2(x, s));
                Eq("x << (s & 0x7FFFFFFF)", x, s, Shl64(x, s), L3(x, s));
                Eq("x << (s & -1)", x, s, Shl64(x, s), L4(x, s));
                Eq("x << ((s & 63) & 255)", x, s, Shl64(x, s), L5(x, s));
                Eq("x << (s + 64)", x, s, Shl64(x, s), L6(x, s));
                Eq("x << (s - 64)", x, s, Shl64(x, s), L7(x, s));
                Eq("x << (s + 128)", x, s, Shl64(x, s), L8(x, s));
                Eq("x << ((s + 64) & 63)", x, s, Shl64(x, s), L9(x, s));
                Eq("x << (64 - s)", x, s, Shl64(x, -s), L10(x, s));
                Eq("x << ((64 - s) & 63)", x, s, Shl64(x, -s), L11(x, s));
                Eq("x << -s", x, s, Shl64(x, -s), L12(x, s));
                Eq("x << (s + 64 + 64 - 128)", x, s, Shl64(x, s), L13(x, s));
                Eq("x << (int)((long)s & 63)", x, s, Shl64(x, s), L14(x, s));
                Eq("x << (int)(long & 63)", x, s, Shl64(x, s), L15(x, s + 0x100000000L));
                Eq("x << (int)long", x, s, Shl64(x, s), L16(x, s + 0x700000000L));
                Eq("x << (s & field 63)", x, s, Shl64(x, s), L17(x, s));
                Eq("x << byte", x, s, Shl64(x, s & 0xFF), L18(x, (byte)(s & 0xFF)));
                Eq("x << sbyte", x, s, Shl64(x, (sbyte)s), L19(x, (sbyte)s));
                Eq("x << (s | 64)", x, s, Shl64(x, s), L20(x, s));
                Eq("x << (s ^ 64)", x, s, Shl64(x, s), L21(x, s));
                Eq("x << (int)((uint)s & 63)", x, s, Shl64(x, s), L22(x, s));

                Eq("x << (s & 31)", x, s, Shl64(x, Norm(s, 32)), M0(x, s));
                Eq("x << (s & 15)", x, s, Shl64(x, Norm(s, 16)), M1(x, s));
                Eq("x << (s & 62)", x, s, Shl64(x, Norm(s, 64) - Norm(s, 2)), M2(x, s));
                Eq("x << (s & 64)", x, s, x, M3(x, s));
                Eq("x << (s + 32)", x, s, Shl64(x, s + 32), M4(x, s));
                Eq("x << (32 - s)", x, s, Shl64(x, 32 - s), M5(x, s));
                Eq("x << (s + 63)", x, s, Shl64(x, s + 63), M6(x, s));
                Eq("x << ((s & 31) + 32)", x, s, Shl64(x, Norm(s, 32) + 32), M7(x, s));
                Eq("x << ((s + 32) & 63)", x, s, Shl64(x, s + 32), M8(x, s));
                Eq("x << (s & 0x3E)", x, s, Shl64(x, Norm(s, 64) - Norm(s, 2)), M9(x, s));
                Eq("x << (s & field 31)", x, s, Shl64(x, Norm(s, 32)), M10(x, s));
                Eq("x << ((s & 63) >> 1)", x, s, Shl64(x, Norm(s, 64) / 2), M11(x, s));
                Eq("x << (s * 2)", x, s, Shl64(x, s * 2), M12(x, s));
                Eq("x << (s + 1)", x, s, Shl64(x, s + 1), M13(x, s));
                Eq("x << (s - 1)", x, s, Shl64(x, s - 1), M14(x, s));
                Eq("x << (63 - s)", x, s, Shl64(x, 63 - s), M15(x, s));
                Eq("x << ~s", x, s, Shl64(x, -s - 1), M16(x, s));

                Eq("x >> s", x, s, Sar64(x, s), R0(x, s));
                Eq("x >> (s & 63)", x, s, Sar64(x, s), R1(x, s));
                Eq("x >> (64 - s)", x, s, Sar64(x, -s), R2(x, s));
                Eq("x >> ((64 - s) & 63)", x, s, Sar64(x, -s), R3(x, s));
                Eq("x >> (s & 31)", x, s, Sar64(x, Norm(s, 32)), R4(x, s));
                Eq("x >> (s + 32)", x, s, Sar64(x, s + 32), R5(x, s));
                Eq("x >> (s + 64)", x, s, Sar64(x, s), R6(x, s));
                Eq("x >> (s & 255)", x, s, Sar64(x, s), R7(x, s));
                Eq("x >>> s", x, s, Shr64(x, s), (long)U0(ux, s));
                Eq("x >>> (s & 63)", x, s, Shr64(x, s), (long)U1(ux, s));
                Eq("x >>> (64 - s)", x, s, Shr64(x, -s), (long)U2(ux, s));
                Eq("x >>> ((64 - s) & 63)", x, s, Shr64(x, -s), (long)U3(ux, s));
                Eq("x >>> (s & 31)", x, s, Shr64(x, Norm(s, 32)), (long)U4(ux, s));
                Eq("x >>> (s - 64)", x, s, Shr64(x, s), (long)U5(ux, s));
                Eq("x >>> (32 - s)", x, s, Shr64(x, 32 - s), (long)U6(ux, s));
                Eq("x >>> ((s & 63) & 63)", x, s, Shr64(x, s), (long)U7(ux, s));

                Eq("int << s", x, s, Shl32(sx, s), I0(xi, s));
                Eq("int << (s & 31)", x, s, Shl32(sx, s), I1(xi, s));
                Eq("int << (s & 63)", x, s, Shl32(sx, s), I2(xi, s));
                Eq("int << (s & 255)", x, s, Shl32(sx, s), I3(xi, s));
                Eq("int << (s + 32)", x, s, Shl32(sx, s), I4(xi, s));
                Eq("int << (s - 32)", x, s, Shl32(sx, s), I5(xi, s));
                Eq("int << (s + 64)", x, s, Shl32(sx, s), I6(xi, s));
                Eq("int << (32 - s)", x, s, Shl32(sx, -s), I7(xi, s));
                Eq("int << ((32 - s) & 31)", x, s, Shl32(sx, -s), I8(xi, s));
                Eq("int << (64 - s)", x, s, Shl32(sx, -s), I9(xi, s));
                Eq("int << -s", x, s, Shl32(sx, -s), I10(xi, s));
                Eq("int << (int)(long & 31)", x, s, Shl32(sx, s), I11(xi, s + 0x300000000L));
                Eq("int << (s & field 31)", x, s, Shl32(sx, s), I12(xi, s));
                Eq("int << (s & 15)", x, s, Shl32(sx, Norm(s, 16)), I13(xi, s));
                Eq("int << (s & 30)", x, s, Shl32(sx, Norm(s, 32) - Norm(s, 2)), I14(xi, s));
                Eq("int << (s + 16)", x, s, Shl32(sx, s + 16), I15(xi, s));
                Eq("int << (16 - s)", x, s, Shl32(sx, 16 - s), I16(xi, s));
                Eq("int << (s + 31)", x, s, Shl32(sx, s + 31), I17(xi, s));
                Eq("int << (s & 32)", x, s, sx, I18(xi, s));
                Eq("int << ((s & 15) + 16)", x, s, Shl32(sx, Norm(s, 16) + 16), I19(xi, s));
                Eq("int >> s", x, s, Sar32(sx, s), IR0(xi, s));
                Eq("int >> (s & 31)", x, s, Sar32(sx, s), IR1(xi, s));
                Eq("int >> (s & 63)", x, s, Sar32(sx, s), IR2(xi, s));
                Eq("int >> (32 - s)", x, s, Sar32(sx, -s), IR3(xi, s));
                Eq("int >> (s + 32)", x, s, Sar32(sx, s), IR4(xi, s));
                Eq("int >> (s & 15)", x, s, Sar32(sx, Norm(s, 16)), IR5(xi, s));
                Eq("int >> (s + 16)", x, s, Sar32(sx, s + 16), IR6(xi, s));
                Eq("uint >> s", x, s, Z32(Shr32(sx, s)), IU0(ui, s));
                Eq("uint >> (s & 31)", x, s, Z32(Shr32(sx, s)), IU1(ui, s));
                Eq("uint >> (s & 63)", x, s, Z32(Shr32(sx, s)), IU2(ui, s));
                Eq("uint >> (32 - s)", x, s, Z32(Shr32(sx, -s)), IU3(ui, s));
                Eq("uint >> ((32 - s) & 31)", x, s, Z32(Shr32(sx, -s)), IU4(ui, s));
                Eq("uint >> (s - 32)", x, s, Z32(Shr32(sx, s)), IU5(ui, s));
                Eq("uint >> (s & 15)", x, s, Z32(Shr32(sx, Norm(s, 16))), IU6(ui, s));
                Eq("uint >> (s + 16)", x, s, Z32(Shr32(sx, s + 16)), IU7(ui, s));

                Eq("(long)(int << s)", x, s, Shl32(sx, s), W0(xi, s));
                Eq("(long)(int >> s) + zext", x, s, Sar32(sx, s) + Z32(Shl32(sx, s)), W1(xi, s));
                Eq("(ulong)(uint >> s) + ...", x, s, Z32(Shr32(sx, s)) + Z32(Shl32(sx, s)), (long)W2(ui, s));
                Eq("k + (long)(int << s)", x, s, 0x100000000L + Shl32(sx, s), W3(xi, s, 0x100000000L));

                Eq("shared 64-bit count", x, s, RShared64(x, ~x, x ^ 0x5A5A5A5A5A5A5A5AL, s), Shared64(x, ~x, x ^ 0x5A5A5A5A5A5A5A5AL, s));
                Eq("shared 32-bit count", x, s, RShared32(sx, S32(~x), S32(x >> 7), s), Shared32(xi, ToInt(~x), ToInt(x >> 7), s));
                Eq("count also a value", x, s, RCountUsed(x, s), CountUsed(x, s));
                Eq("wide count also a value", x, s, RCountUsed32(sx, s), CountUsed32(xi, s));

                Eq("rotate by hand, masked", x, s, Rol64(x, s), (long)Rot0(ux, s));
                Eq("rotate by hand", x, s, Rol64(x, s), (long)Rot1(ux, s));
                Eq("RotateLeft", x, s, Rol64(x, s), (long)Rot2(ux, s));
                Eq("RotateLeft, masked", x, s, Rol64(x, s), (long)Rot3(ux, s));
                Eq("RotateRight(64 - s)", x, s, Rol64(x, s), (long)Rot4(ux, s));
                Eq("RotateLeft(s + 64)", x, s, Rol64(x, s), (long)Rot5(ux, s));
                Eq("rotate by hand, -s", x, s, Rol64(x, s), (long)Rot6(ux, s));
                Eq("32-bit rotate by hand, masked", x, s, Z32(Rol32(sx, s)), Rot32a(ui, s));
                Eq("32-bit rotate by hand", x, s, Z32(Rol32(sx, s)), Rot32b(ui, s));
                Eq("32-bit RotateLeft", x, s, Z32(Rol32(sx, s)), Rot32c(ui, s));
                Eq("32-bit RotateLeft, & 63", x, s, Z32(Rol32(sx, s)), Rot32d(ui, s));
                Eq("32-bit RotateRight(32 - s)", x, s, Z32(Rol32(sx, s)), Rot32e(ui, s));
                Eq("32-bit RotateLeft(s + 32)", x, s, Z32(Rol32(sx, s)), Rot32f(ui, s));
                Eq("32-bit RotateLeft(s + 64)", x, s, Z32(Rol32(sx, s)), Rot32g(ui, s));

                long bit64 = Shl64(1, s), bit32 = Shl32(1, s);
                Eq("bit test", x, s, Sar64(x, s) & 1, Bit0(x, s));
                Eq("bit set", x, s, x | bit64, Bit1(x, s));
                Eq("bit clear", x, s, x & ~bit64, Bit2(x, s));
                Eq("bit flip, s + 64", x, s, x ^ bit64, Bit3(x, s));
                Eq("32-bit bit test", x, s, Sar32(sx, s) & 1, Bit4(xi, s));
                Eq("32-bit bit set", x, s, sx | bit32, Bit5(xi, s));
                Eq("32-bit bit clear", x, s, sx & ~bit32, Bit6(xi, s));
                Eq("32-bit bit flip, s + 32", x, s, sx ^ bit32, Bit7(xi, s));
                Eq("bit test, compare", x, s, B((x & bit64) != 0), B(Bit8(x, s)));
                Eq("32-bit bit test, compare", x, s, B((sx & bit32) != 0), B(Bit9(xi, s)));
                Eq("k + (x << s)", x, s, 77 + Shl64(x, s), ShAdd0(77, x, s));
                Eq("k + ((long)int << s)", x, s, 77 + Shl64(sx, s), ShAdd1(77, xi, s));
                Eq("k + ((long)uint << s)", x, s, 77 + Shl64(Z32(x), s), ShAdd2(77, ui, s));
            }

            for (int s = -70; s <= 70; s += 7)
                Eq("shifts in a loop", x, s, RInLoop(vec, s), InLoop(vec, s));

            Eq("late constant 32-bit counts", x, 0, RLate32(sx, Z32(x)), Late32(xi, ui, Opaque(1) == 1));
            Eq("late constant 64-bit counts", x, 0, RLate64(x, x), Late64(x, ux, Opaque(1) == 1));
            Eq("counts read back from a field", x, 0, RLateField(sx, x), LateField(xi, x));
        }
    }

    static int Main()
    {
        Tab = new long[16];
        for (int i = 0; i < 16; i++) Tab[i] = (i + 3) * 0x0101010101010101L;
        Run();
        Console.WriteLine(fails == 0 ? "ALL PASS (" + checks + " checks)" : "FAILED " + fails + " of " + checks);
        return fails == 0 ? 0 : 1;
    }
}
