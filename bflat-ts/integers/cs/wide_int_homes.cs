// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Integer locals of four bytes or less in a stack home of eight bytes.
//
// On a zkVM target (JitZkBumpAddr set) an int, short or byte local that is
// not address-taken gets a whole aligned 8-byte slot, and so does an int
// spill temp. A store to the slot writes the whole register (sd) and a
// reload of a register candidate reads it back bit for bit (ld) in place of
// a load that extends the low half. What can go wrong: code after the reload
// that needs the upper half to be an extension of the lower one, a wide
// store to a home that is in fact narrow (an address-taken local, a struct
// field, a stack parameter) and overwrites its neighbour, a narrow store
// followed by a wide load that reads stale upper bytes, and a partial write
// to the low bytes of a local that is widened by mistake.
//
// The values are the extremes of each width with a dirty upper half. They
// reach a local through different instructions (a 64-bit add, a logical
// shift, a zero-extending load, a call result) so that the register is not
// always in its canonical form. Each local is then forced to its home (more
// live values than registers across a call, a handler that reads them, or a
// method without optimization) and used in ways that depend on the upper
// half: widening to long and ulong, division, shifts, signed and unsigned
// compares, an argument of a call. The expected results are computed from
// the 64-bit source with 64-bit arithmetic only.
//
// The stack is filled with a pattern before each case, so a load that reads
// bytes no store has written does not see zeros by luck.
//
// Each case prints one line; the run ends with ALL PASS or a FAIL count, and
// the guest returns the number of failures.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class E : Exception { public int Code; public E(int c) : base("e") { Code = c; } }

struct P2 { public int X; public int Y; }

struct S4 { public int A; public short B; public byte C; public bool D; }

// More fields than the JIT promotes: the fields stay in the memory of the
// struct, next to each other.
struct W9 { public int A; public int B; public short C; public short D; public byte E; public byte F; public byte G; public byte H; public int I; }

[StructLayout(LayoutKind.Sequential, Size = 4096)] struct Big { public long First; }

static unsafe class Program
{
    static int fails;
    static long sink;

    static void Check(string name, int bad)
    {
        Console.WriteLine((bad == 0 ? "ok   " : "FAIL ") + name + (bad == 0 ? "" : " cases=0x" + bad.ToString("x")));
        if (bad != 0) fails++;
    }

    static readonly long[] R =
    {
        0, 1, -1, int.MinValue, 0x80000000, 0xFFFFFFFF, 0x7FFFFFFF, 0x100000000,
        0x123456789ABCDEF0, unchecked((long)0xFEDCBA9876543210), 0x180000000, long.MaxValue, long.MinValue,
        unchecked((long)0xFFFFFFFF00000000), 0x5A5A5A5A00008000, 0x7FFFFFFF0000FFFF, 0x1234567800000080,
        unchecked((long)0x80000000000000FF), 0x7F, 0x100, 0x7FFF, 0x10000, unchecked((long)0xAAAAAAAA55555555),
        0x55555555AAAAAAAA,
    };

    // ---- the reference: 64-bit arithmetic only ----

    static long Sx(long raw, int bits) => (raw << (64 - bits)) >> (64 - bits);

    static long Zx(long raw, int bits) => (long)((ulong)(raw << (64 - bits)) >> (64 - bits));

    // What an int that holds the low half of raw gives when it is used.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool WideI(long raw, long sx, ulong zx, long div, long shr, bool neg)
    {
        long want = Sx(raw, 32);
        return sx == want && zx == (ulong)Zx(raw, 32) && div == want / 3 && shr == want >> 31 && neg == want < 0;
    }

    // The same for a uint.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool WideU(long raw, ulong zx, long sx, ulong div, ulong shr, bool big)
    {
        ulong want = (ulong)Zx(raw, 32);
        return zx == want && sx == Sx(raw, 32) && div == want / 3 && shr == want >> 31 && big == want > 0x7FFFFFFF;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Narrow(long raw, long got, int bits, bool signed) => got == (signed ? Sx(raw, bits) : Zx(raw, bits));

    // The value as an argument: the callee widens it.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool ArgI(long raw, int v) => v == Sx(raw, 32) && (uint)v == (ulong)Zx(raw, 32);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool ArgU(long raw, uint v) => v == (ulong)Zx(raw, 32) && (int)v == Sx(raw, 32);

    // ---- sources and calls ----

    [MethodImpl(MethodImplOptions.NoInlining)] static int RetInt(long x) => (int)x;
    [MethodImpl(MethodImplOptions.NoInlining)] static uint RetUInt(long x) => (uint)x;
    [MethodImpl(MethodImplOptions.NoInlining)] static long RetLong(long x) => x;
    [MethodImpl(MethodImplOptions.NoInlining)] static byte RetByte(long x) => (byte)x;
    [MethodImpl(MethodImplOptions.NoInlining)] static sbyte RetSByte(long x) => (sbyte)x;
    [MethodImpl(MethodImplOptions.NoInlining)] static short RetShort(long x) => (short)x;
    [MethodImpl(MethodImplOptions.NoInlining)] static ushort RetUShort(long x) => (ushort)x;

    // Overwrites every caller-saved register with values made from x.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Clobber(long x)
    {
        long a = x ^ 0x5555555555555555, b = a * 3 + 1, c = b ^ (a >> 3), d = c + b * 7;
        long e = d ^ (c << 5), f = e + d * 11, g = f ^ (e >> 7), h = g + f * 13;
        long i = h ^ (g << 9), j = i + h * 17, k = j ^ (i >> 11), l = k + j * 19;
        long m = l ^ (k << 13), n = m + l * 23, o = n ^ (m >> 2), p = o + n * 29;
        sink += a ^ b ^ c ^ d ^ e ^ f ^ g ^ h ^ i ^ j ^ k ^ l ^ m ^ n ^ o ^ p;
        return x + 1;
    }

    // Overwrites the registers, then throws for a positive code.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Thrower(int code)
    {
        long r = Clobber(code);
        if (code > 0) throw new E(code);
        return r;
    }

    // Leaves a pattern in the stack below the caller.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Dirty()
    {
        Span<long> s = stackalloc long[1024];
        s.Fill(0x5A5A5A5AA5A5A5A5);
        sink += s[511];
    }

    // ---- int and uint locals ----

    // Sixteen values cross the call: more than the callee-saved registers,
    // so some of the locals are spilled and reloaded. z is zero.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int IntHomes(long[] s, uint[] lo, int o, long z)
    {
        long r0 = s[o], r1 = s[o + 1], r2 = s[o + 2], r3 = s[o + 3];
        int i0 = (int)(r0 + z);
        int i1 = (int)((ulong)r1 >> (int)z);
        int i2 = (int)lo[o + 2];
        int i3 = RetInt(r3);
        int i4 = (int)RetUInt(r0);
        int i5 = (int)(r2 * (z + 1));
        uint u0 = (uint)(r0 ^ z);
        uint u1 = (uint)RetLong(r1);
        uint u2 = lo[o + 2];
        uint u3 = RetUInt(r3);
        uint u4 = (uint)RetInt(r1);
        uint u5 = (uint)(r3 - z);
        Clobber(z);
        int bad = 0;
        if (!WideI(r0, i0, (uint)i0, i0 / 3, i0 >> 31, i0 < 0)) bad |= 1 << 0;
        if (!WideI(r1, i1, (uint)i1, i1 / 3, i1 >> 31, i1 < 0)) bad |= 1 << 1;
        if (!WideI(r2, i2, (uint)i2, i2 / 3, i2 >> 31, i2 < 0)) bad |= 1 << 2;
        if (!WideI(r3, i3, (uint)i3, i3 / 3, i3 >> 31, i3 < 0)) bad |= 1 << 3;
        if (!WideI(r0, i4, (uint)i4, i4 / 3, i4 >> 31, i4 < 0)) bad |= 1 << 4;
        if (!WideI(r2, i5, (uint)i5, i5 / 3, i5 >> 31, i5 < 0)) bad |= 1 << 5;
        if (!WideU(r0, u0, (int)u0, u0 / 3, u0 >> 31, u0 > 0x7FFFFFFFu)) bad |= 1 << 6;
        if (!WideU(r1, u1, (int)u1, u1 / 3, u1 >> 31, u1 > 0x7FFFFFFFu)) bad |= 1 << 7;
        if (!WideU(r2, u2, (int)u2, u2 / 3, u2 >> 31, u2 > 0x7FFFFFFFu)) bad |= 1 << 8;
        if (!WideU(r3, u3, (int)u3, u3 / 3, u3 >> 31, u3 > 0x7FFFFFFFu)) bad |= 1 << 9;
        if (!WideU(r1, u4, (int)u4, u4 / 3, u4 >> 31, u4 > 0x7FFFFFFFu)) bad |= 1 << 10;
        if (!WideU(r3, u5, (int)u5, u5 / 3, u5 >> 31, u5 > 0x7FFFFFFFu)) bad |= 1 << 11;
        if (!ArgI(r0, i0) || !ArgI(r1, i1) || !ArgI(r2, i2) || !ArgI(r3, i3) || !ArgI(r0, i4) || !ArgI(r2, i5))
            bad |= 1 << 12;
        if (!ArgU(r0, u0) || !ArgU(r1, u1) || !ArgU(r2, u2) || !ArgU(r3, u3) || !ArgU(r1, u4) || !ArgU(r3, u5))
            bad |= 1 << 13;
        return bad;
    }

    // The handler reads every local, so each one has its home up to date
    // and is reloaded from it. i5 and u5 are set again inside the try.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int IntHomesEH(long[] s, uint[] lo, int o, long z, int code)
    {
        long r0 = s[o], r1 = s[o + 1], r2 = s[o + 2], r3 = s[o + 3];
        int i0 = (int)(r0 + z);
        int i1 = (int)((ulong)r1 >> (int)z);
        int i2 = (int)lo[o + 2];
        int i3 = RetInt(r3);
        int i4 = (int)RetUInt(r0);
        int i5 = 77;
        uint u0 = (uint)(r0 ^ z);
        uint u1 = (uint)RetLong(r1);
        uint u2 = lo[o + 2];
        uint u3 = RetUInt(r3);
        uint u4 = (uint)RetInt(r1);
        uint u5 = 78;
        int bad = 0, caught = 0;
        try
        {
            i5 = (int)(r2 * (z + 1));
            u5 = (uint)(r3 - z);
            Thrower(code);
            i4 = RetInt(r2);
            u4 = RetUInt(r2);
        }
        catch (E e)
        {
            caught = e.Code;
            if (!WideI(r0, i0, (uint)i0, i0 / 3, i0 >> 31, i0 < 0)) bad |= 1 << 14;
            if (!WideI(r2, i5, (uint)i5, i5 / 3, i5 >> 31, i5 < 0)) bad |= 1 << 15;
            if (!WideU(r1, u1, (int)u1, u1 / 3, u1 >> 31, u1 > 0x7FFFFFFFu)) bad |= 1 << 16;
            if (!WideU(r3, u5, (int)u5, u5 / 3, u5 >> 31, u5 > 0x7FFFFFFFu)) bad |= 1 << 17;
            if (!ArgI(r1, i1) || !ArgU(r2, u2)) bad |= 1 << 18;
        }
        long r4i = code > 0 ? r0 : r2, r4u = code > 0 ? r1 : r2;
        if (caught != (code > 0 ? code : 0)) bad |= 1 << 19;
        if (!WideI(r0, i0, (uint)i0, i0 / 3, i0 >> 31, i0 < 0)) bad |= 1 << 0;
        if (!WideI(r1, i1, (uint)i1, i1 / 3, i1 >> 31, i1 < 0)) bad |= 1 << 1;
        if (!WideI(r2, i2, (uint)i2, i2 / 3, i2 >> 31, i2 < 0)) bad |= 1 << 2;
        if (!WideI(r3, i3, (uint)i3, i3 / 3, i3 >> 31, i3 < 0)) bad |= 1 << 3;
        if (!WideI(r4i, i4, (uint)i4, i4 / 3, i4 >> 31, i4 < 0)) bad |= 1 << 4;
        if (!WideI(r2, i5, (uint)i5, i5 / 3, i5 >> 31, i5 < 0)) bad |= 1 << 5;
        if (!WideU(r0, u0, (int)u0, u0 / 3, u0 >> 31, u0 > 0x7FFFFFFFu)) bad |= 1 << 6;
        if (!WideU(r1, u1, (int)u1, u1 / 3, u1 >> 31, u1 > 0x7FFFFFFFu)) bad |= 1 << 7;
        if (!WideU(r2, u2, (int)u2, u2 / 3, u2 >> 31, u2 > 0x7FFFFFFFu)) bad |= 1 << 8;
        if (!WideU(r3, u3, (int)u3, u3 / 3, u3 >> 31, u3 > 0x7FFFFFFFu)) bad |= 1 << 9;
        if (!WideU(r4u, u4, (int)u4, u4 / 3, u4 >> 31, u4 > 0x7FFFFFFFu)) bad |= 1 << 10;
        if (!WideU(r3, u5, (int)u5, u5 / 3, u5 >> 31, u5 > 0x7FFFFFFFu)) bad |= 1 << 11;
        if (!ArgI(r0, i0) || !ArgI(r1, i1) || !ArgI(r2, i2) || !ArgI(r3, i3) || !ArgI(r4i, i4) || !ArgI(r2, i5))
            bad |= 1 << 12;
        if (!ArgU(r0, u0) || !ArgU(r1, u1) || !ArgU(r2, u2) || !ArgU(r3, u3) || !ArgU(r4u, u4) || !ArgU(r3, u5))
            bad |= 1 << 13;
        return bad;
    }

    // Without optimization every local lives in its home and is loaded with
    // a narrow load at each use, after a wide store.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static int IntHomesNoOpt(long[] s, uint[] lo, int o, long z)
    {
        long r0 = s[o], r1 = s[o + 1], r2 = s[o + 2], r3 = s[o + 3];
        int i0 = (int)(r0 + z);
        int i1 = (int)((ulong)r1 >> (int)z);
        int i2 = (int)lo[o + 2];
        int i3 = RetInt(r3);
        int i4 = (int)RetUInt(r0);
        int i5 = (int)(r2 * (z + 1));
        uint u0 = (uint)(r0 ^ z);
        uint u1 = (uint)RetLong(r1);
        uint u2 = lo[o + 2];
        uint u3 = RetUInt(r3);
        uint u4 = (uint)RetInt(r1);
        uint u5 = (uint)(r3 - z);
        Clobber(z);
        int bad = 0;
        if (!WideI(r0, i0, (uint)i0, i0 / 3, i0 >> 31, i0 < 0)) bad |= 1 << 0;
        if (!WideI(r1, i1, (uint)i1, i1 / 3, i1 >> 31, i1 < 0)) bad |= 1 << 1;
        if (!WideI(r2, i2, (uint)i2, i2 / 3, i2 >> 31, i2 < 0)) bad |= 1 << 2;
        if (!WideI(r3, i3, (uint)i3, i3 / 3, i3 >> 31, i3 < 0)) bad |= 1 << 3;
        if (!WideI(r0, i4, (uint)i4, i4 / 3, i4 >> 31, i4 < 0)) bad |= 1 << 4;
        if (!WideI(r2, i5, (uint)i5, i5 / 3, i5 >> 31, i5 < 0)) bad |= 1 << 5;
        if (!WideU(r0, u0, (int)u0, u0 / 3, u0 >> 31, u0 > 0x7FFFFFFFu)) bad |= 1 << 6;
        if (!WideU(r1, u1, (int)u1, u1 / 3, u1 >> 31, u1 > 0x7FFFFFFFu)) bad |= 1 << 7;
        if (!WideU(r2, u2, (int)u2, u2 / 3, u2 >> 31, u2 > 0x7FFFFFFFu)) bad |= 1 << 8;
        if (!WideU(r3, u3, (int)u3, u3 / 3, u3 >> 31, u3 > 0x7FFFFFFFu)) bad |= 1 << 9;
        if (!WideU(r1, u4, (int)u4, u4 / 3, u4 >> 31, u4 > 0x7FFFFFFFu)) bad |= 1 << 10;
        if (!WideU(r3, u5, (int)u5, u5 / 3, u5 >> 31, u5 > 0x7FFFFFFFu)) bad |= 1 << 11;
        if (!ArgI(r0, i0) || !ArgI(r1, i1) || !ArgI(r2, i2) || !ArgI(r3, i3) || !ArgI(r0, i4) || !ArgI(r2, i5))
            bad |= 1 << 12;
        if (!ArgU(r0, u0) || !ArgU(r1, u1) || !ArgU(r2, u2) || !ArgU(r3, u3) || !ArgU(r1, u4) || !ArgU(r3, u5))
            bad |= 1 << 13;
        return bad;
    }

    // ---- byte, short, char and bool locals ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int SmallHomes(long[] s, int o, long z, int code)
    {
        long r0 = s[o], r1 = s[o + 1], r2 = s[o + 2];
        byte b0 = (byte)(r0 + z);
        sbyte c0 = (sbyte)(r0 ^ z);
        short h0 = (short)(r1 + z);
        ushort w0 = (ushort)(r1 ^ z);
        char k0 = (char)(r2 + z);
        bool t0 = ((r2 >> (int)z) & 1) != 0;
        byte b1 = RetByte(r1);
        sbyte c1 = RetSByte(r2);
        short h1 = RetShort(r2);
        ushort w1 = RetUShort(r0);
        char k1 = (char)RetLong(r0);
        bool t1 = RetLong(r1 & 0x80) != 0;
        int bad = 0;
        try
        {
            Thrower(code);
            b1 = (byte)RetLong(r2);
            h1 = (short)RetInt(r0);
        }
        catch (E)
        {
            if (!Narrow(r0, b0, 8, false) || !Narrow(r2, c1, 8, true) || !Narrow(r2, h1, 16, true)) bad |= 1 << 12;
            c1 = (sbyte)(r1 - z);
            w1 = (ushort)RetInt(r2);
        }
        bool thrown = code > 0;
        if (!Narrow(r0, b0, 8, false)) bad |= 1 << 0;
        if (!Narrow(r0, c0, 8, true)) bad |= 1 << 1;
        if (!Narrow(r1, h0, 16, true)) bad |= 1 << 2;
        if (!Narrow(r1, w0, 16, false)) bad |= 1 << 3;
        if (!Narrow(r2, k0, 16, false)) bad |= 1 << 4;
        if (!Narrow(r2, t0 ? 1 : 0, 1, false)) bad |= 1 << 5;
        if (!Narrow(thrown ? r1 : r2, b1, 8, false)) bad |= 1 << 6;
        if (!Narrow(thrown ? r1 : r2, c1, 8, true)) bad |= 1 << 7;
        if (!Narrow(thrown ? r2 : r0, h1, 16, true)) bad |= 1 << 8;
        if (!Narrow(thrown ? r2 : r0, w1, 16, false)) bad |= 1 << 9;
        if (!Narrow(r0, k1, 16, false)) bad |= 1 << 10;
        if (!Narrow(r1 >> 7, t1 ? 1 : 0, 1, false)) bad |= 1 << 11;
        // Arithmetic on the small values is done in 32 bits.
        if (b0 + c0 + h0 + w0 + k0 != Zx(r0, 8) + Sx(r0, 8) + Sx(r1, 16) + Zx(r1, 16) + Zx(r2, 16)) bad |= 1 << 13;
        return bad;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static int SmallHomesNoOpt(long[] s, int o, long z)
    {
        long r0 = s[o], r1 = s[o + 1], r2 = s[o + 2];
        byte b0 = (byte)(r0 + z);
        sbyte c0 = (sbyte)(r0 ^ z);
        short h0 = (short)(r1 + z);
        ushort w0 = (ushort)(r1 ^ z);
        char k0 = (char)(r2 + z);
        bool t0 = ((r2 >> (int)z) & 1) != 0;
        byte b1 = RetByte(r1);
        sbyte c1 = RetSByte(r2);
        short h1 = RetShort(r2);
        ushort w1 = RetUShort(r0);
        char k1 = (char)RetLong(r0);
        bool t1 = RetLong(r1 & 0x80) != 0;
        Clobber(z);
        int bad = 0;
        if (!Narrow(r0, b0, 8, false)) bad |= 1 << 0;
        if (!Narrow(r0, c0, 8, true)) bad |= 1 << 1;
        if (!Narrow(r1, h0, 16, true)) bad |= 1 << 2;
        if (!Narrow(r1, w0, 16, false)) bad |= 1 << 3;
        if (!Narrow(r2, k0, 16, false)) bad |= 1 << 4;
        if (!Narrow(r2, t0 ? 1 : 0, 1, false)) bad |= 1 << 5;
        if (!Narrow(r1, b1, 8, false)) bad |= 1 << 6;
        if (!Narrow(r2, c1, 8, true)) bad |= 1 << 7;
        if (!Narrow(r2, h1, 16, true)) bad |= 1 << 8;
        if (!Narrow(r0, w1, 16, false)) bad |= 1 << 9;
        if (!Narrow(r0, k1, 16, false)) bad |= 1 << 10;
        if (!Narrow(r1 >> 7, t1 ? 1 : 0, 1, false)) bad |= 1 << 11;
        if (b0 + c0 + h0 + w0 + k0 != Zx(r0, 8) + Sx(r0, 8) + Sx(r1, 16) + Zx(r1, 16) + Zx(r2, 16)) bad |= 1 << 13;
        return bad;
    }

    // ---- parameters ----

    // The 64-bit sources of the eight parameters below.
    static long q0, q1, q2, q3, q4, q5, q6, q7;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int CheckParams(int a, uint b, short c, ushort d, byte e, sbyte f, char g, bool h)
    {
        int bad = 0;
        if (!WideI(q0, a, (uint)a, a / 3, a >> 31, a < 0)) bad |= 1 << 0;
        if (!WideU(q1, b, (int)b, b / 3, b >> 31, b > 0x7FFFFFFFu)) bad |= 1 << 1;
        if (!Narrow(q2, c, 16, true)) bad |= 1 << 2;
        if (!Narrow(q3, d, 16, false)) bad |= 1 << 3;
        if (!Narrow(q4, e, 8, false)) bad |= 1 << 4;
        if (!Narrow(q5, f, 8, true)) bad |= 1 << 5;
        if (!Narrow(q6, g, 16, false)) bad |= 1 << 6;
        if (!Narrow(q7, h ? 1 : 0, 1, false)) bad |= 1 << 7;
        return bad;
    }

    // Parameters in registers with six more values across the call, so some
    // of them are homed. The second half sets the parameters again.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ParamHomes(int a, uint b, short c, ushort d, byte e, sbyte f, char g, bool h)
    {
        long x0 = q0 + 1, x1 = q1 + 2, x2 = q2 + 3, x3 = q3 + 4, x4 = q4 + 5, x5 = q5 + 6;
        Clobber(x0);
        int bad = CheckParams(a, b, c, d, e, f, g, h);
        if (!WideI(q0, a, (uint)a, a / 3, a >> 31, a < 0)) bad |= 1 << 8;
        if (!WideU(q1, b, (int)b, b / 3, b >> 31, b > 0x7FFFFFFFu)) bad |= 1 << 9;
        if (!Narrow(q2, c, 16, true) || !Narrow(q3, d, 16, false) || !Narrow(q4, e, 8, false)) bad |= 1 << 10;
        if (!Narrow(q5, f, 8, true) || !Narrow(q6, g, 16, false) || !Narrow(q7, h ? 1 : 0, 1, false)) bad |= 1 << 11;
        a = (int)(q1 + x0 - q0 - 1);
        b = (uint)(q0 ^ (x1 - q1 - 2));
        c = (short)RetLong(q3);
        e = (byte)(q5 + x2 - q2 - 3);
        f = (sbyte)RetInt(q4);
        Clobber(x1);
        if (!WideI(q1, a, (uint)a, a / 3, a >> 31, a < 0)) bad |= 1 << 12;
        if (!WideU(q0, b, (int)b, b / 3, b >> 31, b > 0x7FFFFFFFu)) bad |= 1 << 13;
        if (!Narrow(q3, c, 16, true) || !Narrow(q5, e, 8, false) || !Narrow(q4, f, 8, true)) bad |= 1 << 14;
        if (!Narrow(q3, d, 16, false) || !Narrow(q6, g, 16, false) || !Narrow(q7, h ? 1 : 0, 1, false)) bad |= 1 << 15;
        if (x0 != q0 + 1 || x1 != q1 + 2 || x2 != q2 + 3 || x3 != q3 + 4 || x4 != q4 + 5 || x5 != q5 + 6) bad |= 1 << 16;
        return bad;
    }

    // The handler reads the parameters: they are homed in the prolog.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ParamHomesEH(int a, uint b, short c, ushort d, byte e, sbyte f, char g, bool h, int code)
    {
        int bad = 0;
        try
        {
            Thrower(code);
            bad = CheckParams(a, b, c, d, e, f, g, h);
        }
        catch (E)
        {
            bad = CheckParams(a, b, c, d, e, f, g, h) << 8;
        }
        if (!WideI(q0, a, (uint)a, a / 3, a >> 31, a < 0)) bad |= 1 << 16;
        if (!WideU(q1, b, (int)b, b / 3, b >> 31, b > 0x7FFFFFFFu)) bad |= 1 << 17;
        if (!Narrow(q2, c, 16, true) || !Narrow(q3, d, 16, false) || !Narrow(q4, e, 8, false)) bad |= 1 << 18;
        if (!Narrow(q5, f, 8, true) || !Narrow(q6, g, 16, false) || !Narrow(q7, h ? 1 : 0, 1, false)) bad |= 1 << 19;
        return bad;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static int ParamHomesNoOpt(int a, uint b, short c, ushort d, byte e, sbyte f, char g, bool h)
    {
        Clobber(a);
        int bad = CheckParams(a, b, c, d, e, f, g, h);
        if (!WideI(q0, a, (uint)a, a / 3, a >> 31, a < 0)) bad |= 1 << 8;
        if (!WideU(q1, b, (int)b, b / 3, b >> 31, b > 0x7FFFFFFFu)) bad |= 1 << 9;
        if (!Narrow(q2, c, 16, true) || !Narrow(q3, d, 16, false) || !Narrow(q4, e, 8, false)) bad |= 1 << 10;
        if (!Narrow(q5, f, 8, true) || !Narrow(q6, g, 16, false) || !Narrow(q7, h ? 1 : 0, 1, false)) bad |= 1 << 11;
        return bad;
    }

    // A frame of more than 4 KiB: the homes of the parameters and of the
    // locals are at offsets that do not fit an instruction.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int LargeFrame(int a, uint b, short c, ushort d, byte e, sbyte f, char g, bool h, int code)
    {
        Big buf = default;
        Span<long> words = MemoryMarshal.CreateSpan(ref buf.First, 512);
        words.Fill(q0);
        int i0 = (int)(q1 + code - code);
        uint u0 = (uint)RetLong(q0);
        int bad = 0;
        try
        {
            Thrower(code);
            i0 = RetInt(q1);
        }
        catch (E)
        {
            bad = CheckParams(a, b, c, d, e, f, g, h) << 8;
            if (!ArgI(q1, i0) || !ArgU(q0, u0)) bad |= 1 << 20;
        }
        bad |= CheckParams(a, b, c, d, e, f, g, h);
        if (!WideI(q1, i0, (uint)i0, i0 / 3, i0 >> 31, i0 < 0)) bad |= 1 << 16;
        if (!WideU(q0, u0, (int)u0, u0 / 3, u0 >> 31, u0 > 0x7FFFFFFFu)) bad |= 1 << 17;
        if (words[0] != q0 || words[511] != q0 || words[256] != q0) bad |= 1 << 18;
        return bad;
    }

    // The same frame size without a handler: the frame has no frame pointer
    // and the homes are addressed from the stack pointer. Fourteen values
    // cross the call.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int LargeSpill(int a, uint b, short c, ushort d, byte e, sbyte f, char g, bool h)
    {
        Big buf = default;
        Span<long> words = MemoryMarshal.CreateSpan(ref buf.First, 512);
        words.Fill(q1);
        int i0 = (int)(q1 + q7 - q7), i1 = (int)((ulong)q2 >> (i0 - i0)), i2 = RetInt(q3);
        uint u0 = (uint)RetLong(q0), u1 = (uint)(q4 ^ (q7 - q7)), u2 = RetUInt(q5);
        Clobber(i0);
        int bad = CheckParams(a, b, c, d, e, f, g, h);
        if (!WideI(q0, a, (uint)a, a / 3, a >> 31, a < 0)) bad |= 1 << 8;
        if (!WideU(q1, b, (int)b, b / 3, b >> 31, b > 0x7FFFFFFFu)) bad |= 1 << 9;
        if (!Narrow(q2, c, 16, true) || !Narrow(q3, d, 16, false) || !Narrow(q4, e, 8, false)) bad |= 1 << 10;
        if (!Narrow(q5, f, 8, true) || !Narrow(q6, g, 16, false) || !Narrow(q7, h ? 1 : 0, 1, false)) bad |= 1 << 11;
        if (!WideI(q1, i0, (uint)i0, i0 / 3, i0 >> 31, i0 < 0)) bad |= 1 << 12;
        if (!WideI(q2, i1, (uint)i1, i1 / 3, i1 >> 31, i1 < 0)) bad |= 1 << 13;
        if (!WideI(q3, i2, (uint)i2, i2 / 3, i2 >> 31, i2 < 0)) bad |= 1 << 14;
        if (!WideU(q0, u0, (int)u0, u0 / 3, u0 >> 31, u0 > 0x7FFFFFFFu)) bad |= 1 << 15;
        if (!WideU(q4, u1, (int)u1, u1 / 3, u1 >> 31, u1 > 0x7FFFFFFFu)) bad |= 1 << 16;
        if (!WideU(q5, u2, (int)u2, u2 / 3, u2 >> 31, u2 > 0x7FFFFFFFu)) bad |= 1 << 17;
        if (words[0] != q1 || words[511] != q1 || words[256] != q1) bad |= 1 << 18;
        return bad;
    }

    // Parameters on the stack have the home the caller made: a store to one
    // of them must not touch the next one.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int StackParams(long f0, long f1, long f2, long f3, long f4, long f5, long f6, long f7,
                           int a, uint b, short c, byte d, int e, long z)
    {
        int bad = 0;
        a = (int)(z - 1);
        c = (short)(z - 1);
        Clobber(z);
        if (a != -1 || c != -1 || b != 0x22222222u || d != 0x44 || e != 0x55555555) bad |= 1;
        b = (uint)z;
        d = (byte)z;
        e = (int)(z + int.MinValue);
        Clobber(z);
        if (a != -1 || c != -1 || b != 0 || d != 0 || e != int.MinValue || (long)e != int.MinValue) bad |= 2;
        if (f0 + f1 + f2 + f3 + f4 + f5 + f6 + f7 != 36) bad |= 4;
        return bad;
    }

    // ---- struct fields ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long SumP2(P2 p) => (long)p.X * 3 + p.Y;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long SumS4(S4 q) => (long)q.A * 3 + q.B * 5 + q.C * 7 + (q.D ? 11 : 0);

    // Two small structs as parameters and the same two as locals. Each
    // struct is one register wide; a field must not be stored wider than it
    // is when the fields share the home of the struct.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int StructHomes(P2 p, S4 q, P2[] ps, S4[] qs, int code, int z)
    {
        P2 l = ps[0];
        S4 m = qs[0];
        int bad = 0;
        p.X = z - 1;
        q.A = z - 1;
        l.X = z - 1;
        m.A = z - 1;
        Clobber(z);
        if (p.X != -1 || p.Y != 0x22222222 || l.X != -1 || l.Y != 0x22222222) bad |= 1;
        if (q.A != -1 || q.B != 0x4444 || q.C != 0x55 || !q.D) bad |= 2;
        if (m.A != -1 || m.B != 0x4444 || m.C != 0x55 || !m.D) bad |= 4;
        p.Y = z;
        l.Y = z;
        q.B = (short)(z - 1);
        m.B = (short)(z - 1);
        try
        {
            Thrower(code);
            p.X = z;
            l.X = z;
            q.C = (byte)(z + 255);
            m.C = (byte)(z + 255);
        }
        catch (E)
        {
            p.Y = z - 1;
            l.Y = z - 1;
            q.D = false;
            m.D = false;
        }
        bool thrown = code > 0;
        if (p.X != (thrown ? -1 : 0) || p.Y != (thrown ? -1 : 0)) bad |= 8;
        if (l.X != p.X || l.Y != p.Y) bad |= 16;
        if (q.A != -1 || q.B != -1 || q.C != (thrown ? 0x55 : 0xFF) || q.D != !thrown) bad |= 32;
        if (m.A != q.A || m.B != q.B || m.C != q.C || m.D != q.D) bad |= 64;
        long wantP = thrown ? -4 : 0, wantQ = -3 - 5 + (thrown ? 0x55 * 7 : 0xFF * 7 + 11);
        if (SumP2(p) != wantP || SumP2(l) != wantP || SumS4(q) != wantQ || SumS4(m) != wantQ) bad |= 128;
        ps[1] = p;
        qs[1] = m;
        if (SumP2(ps[1]) != wantP || SumS4(qs[1]) != wantQ) bad |= 256;
        return bad;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long HashW(ref W9 w)
        => w.A + w.B * 3L + w.C * 5L + w.D * 7L + w.E * 11L + w.F * 13L + w.G * 17L + w.H * 19L + w.I * 23L;

    static long HashOf(long a, long b, long c, long d, long e, long f, long g, long h, long i)
        => a + b * 3 + c * 5 + d * 7 + e * 11 + f * 13 + g * 17 + h * 19 + i * 23;

    // Fields that stay in the memory of the struct: every store is as wide
    // as the field.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int MemoryStruct(int z)
    {
        W9 w = default;
        int bad = 0;
        w.I = z + 9; w.H = (byte)(z + 8); w.G = (byte)(z + 7); w.F = (byte)(z + 6); w.E = (byte)(z + 5);
        w.D = (short)(z + 4); w.C = (short)(z + 3); w.B = z + 2; w.A = z + 1;
        if (HashW(ref w) != HashOf(1, 2, 3, 4, 5, 6, 7, 8, 9)) bad |= 1;
        w.A = z - 1;
        if (HashW(ref w) != HashOf(-1, 2, 3, 4, 5, 6, 7, 8, 9) || w.B != 2) bad |= 2;
        w.C = (short)(z - 1);
        if (HashW(ref w) != HashOf(-1, 2, -1, 4, 5, 6, 7, 8, 9) || w.D != 4) bad |= 4;
        w.E = (byte)(z + 255); w.G = (byte)(z + 255);
        if (HashW(ref w) != HashOf(-1, 2, -1, 4, 255, 6, 255, 8, 9) || w.F != 6 || w.H != 8) bad |= 8;
        w.B = z - 1; w.A = z; w.D = (short)(z - 1); w.C = (short)z; w.H = (byte)(z + 255); w.I = z + int.MinValue;
        if (HashW(ref w) != HashOf(0, -1, 0, -1, 255, 6, 255, 255, int.MinValue)) bad |= 16;
        return bad;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static int MemoryStructNoOpt(int z)
    {
        W9 w = default;
        P2 p = default;
        S4 q = default;
        int bad = 0;
        w.I = z + 9; w.H = (byte)(z + 8); w.G = (byte)(z + 7); w.F = (byte)(z + 6); w.E = (byte)(z + 5);
        w.D = (short)(z + 4); w.C = (short)(z + 3); w.B = z + 2; w.A = z + 1;
        if (HashW(ref w) != HashOf(1, 2, 3, 4, 5, 6, 7, 8, 9)) bad |= 1;
        w.A = z - 1;
        if (HashW(ref w) != HashOf(-1, 2, 3, 4, 5, 6, 7, 8, 9) || w.B != 2) bad |= 2;
        w.C = (short)(z - 1);
        if (HashW(ref w) != HashOf(-1, 2, -1, 4, 5, 6, 7, 8, 9) || w.D != 4) bad |= 4;
        w.E = (byte)(z + 255); w.G = (byte)(z + 255);
        if (HashW(ref w) != HashOf(-1, 2, -1, 4, 255, 6, 255, 8, 9) || w.F != 6 || w.H != 8) bad |= 8;
        w.B = z - 1; w.A = z; w.D = (short)(z - 1); w.C = (short)z; w.H = (byte)(z + 255); w.I = z + int.MinValue;
        if (HashW(ref w) != HashOf(0, -1, 0, -1, 255, 6, 255, 255, int.MinValue)) bad |= 16;
        p.Y = 0x22222222 + z; p.X = z - 1;
        q.D = true; q.C = (byte)(z + 0x55); q.B = (short)(z + 0x4444); q.A = z - 1;
        if (SumP2(p) != -3 + 0x22222222 || SumS4(q) != -3 + 0x4444 * 5 + 0x55 * 7 + 11) bad |= 32;
        p.X = z; q.A = z; q.B = (short)(z - 1);
        if (SumP2(p) != 0x22222222 || SumS4(q) != -5 + 0x55 * 7 + 11) bad |= 64;
        return bad;
    }

    // ---- address-taken locals ----

    [MethodImpl(MethodImplOptions.NoInlining)] static void SetRef(ref int x, int v) { x = v; }
    [MethodImpl(MethodImplOptions.NoInlining)] static void SetOut(out int x, int v) { x = v; }
    [MethodImpl(MethodImplOptions.NoInlining)] static int Get(ref int x) => x;

    // Address-taken locals keep a home of their own width between locals
    // with a wide home. Each kind is written with all ones while the other
    // kind is checked.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static int AddressTakenNoOpt(int z)
    {
        int x0 = z + 1, x1 = z + 2, x2 = z + 3, x3 = z + 4, x4 = z + 5, x5 = z + 6, x6 = z + 7;
        short h = (short)(z + 8);
        byte b = (byte)(z + 9);
        int bad = 0;
        SetRef(ref x1, -1);
        SetOut(out x3, int.MinValue);
        int* p5 = &x5;
        short* ph = &h;
        byte* pb = &b;
        *p5 = 0x7FFFFFFF;
        x0 = z - 1; x2 = z - 1; x4 = z - 1; x6 = z - 1;
        if (Get(ref x1) != -1 || x3 != int.MinValue || *p5 != 0x7FFFFFFF || *ph != 8 || *pb != 9) bad |= 1;
        SetRef(ref x1, 0);
        SetOut(out x3, 0);
        *p5 = 0; *ph = 0; *pb = 0;
        if (x0 != -1 || x2 != -1 || x4 != -1 || x6 != -1) bad |= 2;
        x1 = z - 1; x3 = z - 1; x5 = z - 1; h = (short)(z - 1); b = (byte)(z + 255);
        x0 = z; x2 = z; x4 = z; x6 = z;
        if (Get(ref x1) != -1 || Get(ref x3) != -1 || *p5 != -1 || *ph != -1 || *pb != 255) bad |= 4;
        x1 = z; x3 = z; x5 = z; h = (short)z; b = (byte)z;
        x0 = z - 1; x2 = z - 1; x4 = z - 1; x6 = z - 1;
        if (Get(ref x1) != 0 || Get(ref x3) != 0 || *p5 != 0 || *ph != 0 || *pb != 0) bad |= 8;
        if (x0 != -1 || x2 != -1 || x4 != -1 || x6 != -1) bad |= 16;
        return bad;
    }

    // The same with optimization: the locals that are not address-taken
    // are read by a handler, so they have homes too.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int AddressTaken(int z, int code)
    {
        int x0 = z + 1, x1 = z + 2, x2 = z + 3, x3 = z + 4, x4 = z + 5, x5 = z + 6, x6 = z + 7;
        int bad = 0;
        SetRef(ref x1, -1);
        SetOut(out x3, int.MinValue);
        int* p5 = &x5;
        *p5 = 0x7FFFFFFF;
        try
        {
            x0 = z - 1; x2 = z - 1; x4 = z - 1; x6 = z - 1;
            Thrower(code);
            SetRef(ref x1, 0);
            SetOut(out x3, 0);
            *p5 = 0;
        }
        catch (E)
        {
            if (Get(ref x1) != -1 || x3 != int.MinValue || *p5 != 0x7FFFFFFF) bad |= 1;
            x1 = z; x3 = z; x5 = z;
        }
        if (x0 != -1 || x2 != -1 || x4 != -1 || x6 != -1) bad |= 2;
        if (Get(ref x1) != 0 || Get(ref x3) != 0 || *p5 != 0) bad |= 4;
        x1 = z - 1; x3 = z - 1; x5 = z - 1;
        x0 = z; x2 = z; x4 = z; x6 = z;
        Clobber(z);
        if (Get(ref x1) != -1 || Get(ref x3) != -1 || *p5 != -1) bad |= 8;
        if (x0 != 0 || x2 != 0 || x4 != 0 || x6 != 0) bad |= 16;
        return bad;
    }

    // ---- one width stored, another read ----

    // A write to the low bytes of an int local keeps its other bytes, and a
    // read of its low bytes sees what a whole store has put there.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Partial(int z)
    {
        int x = 0x11223344 + z, y = z - 1, w = z;
        uint u = 0xA1B2C3D4 + (uint)z;
        int bad = 0;
        Clobber(z);
        if (*(byte*)&x != 0x44 || *(short*)&x != 0x3344 || *(sbyte*)&y != -1 || *(ushort*)&u != 0xC3D4) bad |= 1;
        if (((byte*)&x)[3] != 0x11 || ((byte*)&u)[2] != 0xB2 || ((short*)&u)[1] != unchecked((short)0xA1B2)) bad |= 2;
        *(byte*)&x = (byte)(0xEE + z);
        Unsafe.As<int, short>(ref y) = (short)z;
        *(sbyte*)&w = (sbyte)(z - 1);
        Unsafe.As<uint, ushort>(ref u) = (ushort)(0xFFFF + z);
        Clobber(z);
        if (x != 0x112233EE || y != unchecked((int)0xFFFF0000) || w != 0xFF || u != 0xA1B2FFFF) bad |= 4;
        if ((long)y != -65536 || (ulong)u != 0xA1B2FFFF || (long)w != 255 || (long)x != 0x112233EE) bad |= 8;
        if (*(sbyte*)&x != -18 || *(ushort*)&y != 0 || *(short*)&u != -1 || Unsafe.As<int, byte>(ref w) != 0xFF) bad |= 16;
        // The same width under another type.
        Unsafe.As<int, uint>(ref y) = 0x80000000 + (uint)z;
        Unsafe.As<uint, int>(ref u) = z - 1;
        Clobber(z);
        if (y != int.MinValue || (long)y != int.MinValue || u != 0xFFFFFFFF || (ulong)u != 0xFFFFFFFF) bad |= 32;
        // The upper bytes one by one.
        ((byte*)&w)[3] = (byte)(0x80 + z);
        ((short*)&x)[1] = (short)(z - 1);
        if (w != unchecked((int)0x800000FF) || (long)w != unchecked((int)0x800000FF) || x != unchecked((int)0xFFFF33EE))
            bad |= 64;
        return bad;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static int PartialNoOpt(int z)
    {
        int x = 0x11223344 + z, y = z - 1, w = z;
        uint u = 0xA1B2C3D4 + (uint)z;
        int bad = 0;
        if (Unsafe.As<int, byte>(ref x) != 0x44 || Unsafe.As<int, short>(ref x) != 0x3344) bad |= 1;
        Unsafe.As<int, byte>(ref x) = (byte)(0xEE + z);
        Unsafe.As<int, short>(ref y) = (short)z;
        Unsafe.As<int, sbyte>(ref w) = (sbyte)(z - 1);
        Unsafe.As<uint, ushort>(ref u) = (ushort)(0xFFFF + z);
        if (x != 0x112233EE || y != unchecked((int)0xFFFF0000) || w != 0xFF || u != 0xA1B2FFFF) bad |= 4;
        if ((long)y != -65536 || (ulong)u != 0xA1B2FFFF || (long)w != 255 || (long)x != 0x112233EE) bad |= 8;
        Unsafe.As<int, uint>(ref y) = 0x80000000 + (uint)z;
        Unsafe.As<uint, int>(ref u) = z - 1;
        if (y != int.MinValue || (long)y != int.MinValue || u != 0xFFFFFFFF || (ulong)u != 0xFFFFFFFF) bad |= 32;
        return bad;
    }

    // ---- spill temps ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Three(long ra, long rb, long rc, int a, uint b, int c)
        => a == Sx(ra, 32) && b == (ulong)Zx(rb, 32) && c == Sx(rc, 32);

    // The result of the first call waits in a spill temp while the second
    // call runs, then both are used as 32-bit values.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Temps(long ra, long rb, long rc)
    {
        long sa = Sx(ra, 32), sb = Sx(rb, 32), za = Zx(ra, 32), zb = Zx(rb, 32);
        int bad = 0;
        if (RetUInt(ra) < RetUInt(rb) != za < zb) bad |= 1 << 0;
        if (RetInt(ra) < RetInt(rb) != sa < sb) bad |= 1 << 1;
        if (RetInt(ra) == (int)RetUInt(rb) != (sa == sb)) bad |= 1 << 2;
        if ((RetInt(ra) ^ RetInt(rb)) != Sx(ra ^ rb, 32)) bad |= 1 << 3;
        if (RetUInt(ra) >> (RetInt(rb) & 31) != (ulong)(za >> (int)(sb & 31))) bad |= 1 << 4;
        if (RetUInt(ra) + RetUInt(rb) != (ulong)Zx(ra + rb, 32)) bad |= 1 << 5;
        if (RetInt(ra) - RetInt(rb) != Sx(ra - rb, 32)) bad |= 1 << 6;
        if (sb != 0 && sb != -1 && RetInt(ra) / RetInt(rb) != sa / sb) bad |= 1 << 7;
        if (zb != 0 && RetUInt(ra) % RetUInt(rb) != (ulong)(za % zb)) bad |= 1 << 8;
        if ((long)RetInt(ra) * RetInt(rb) + RetUInt(rc) != sa * sb + Zx(rc, 32)) bad |= 1 << 9;
        if (!Three(ra, rb, rc, RetInt(ra), RetUInt(rb), RetInt(rc))) bad |= 1 << 10;
        if ((byte)RetInt(ra) + RetShort(rb) * RetSByte(rc) != Zx(ra, 8) + Sx(rb, 16) * Sx(rc, 8)) bad |= 1 << 11;
        return bad;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Fold14(int a0, uint a1, int a2, uint a3, int a4, uint a5, int a6, uint a7,
                       int a8, uint a9, int a10, uint a11, int a12, uint a13)
        => a0 + a1 * 2L + a2 * 3L + a3 * 4L + a4 * 5L + a5 * 6L + a6 * 7L + a7 * 8L
           + a8 * 9L + a9 * 10L + a10 * 11L + a11 * 12L + a12 * 13L + a13 * 14L;

    // Fourteen results wait for the last call: more than the callee-saved
    // registers, so some of them wait in spill temps.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ManyTemps(long[] s, int o)
    {
        int bad = 0;
        long got = Fold14(RetInt(s[o]), RetUInt(s[o + 1]), RetInt(s[o + 2]), RetUInt(s[o + 3]), RetInt(s[o + 4]),
                          RetUInt(s[o + 5]), RetInt(s[o + 6]), RetUInt(s[o + 7]), RetInt(s[o + 8]), RetUInt(s[o + 9]),
                          RetInt(s[o + 10]), RetUInt(s[o + 11]), RetInt(s[o + 12]), RetUInt(s[o + 13]));
        long want = 0;
        for (int k = 0; k < 14; k++)
            want += ((k & 1) == 0 ? Sx(s[o + k], 32) : Zx(s[o + k], 32)) * (k + 1);
        if (got != want) bad |= 1;

        int x = RetInt(s[o]) ^ (RetInt(s[o + 1]) - (RetInt(s[o + 2]) ^ (RetInt(s[o + 3]) - (RetInt(s[o + 4])
                ^ (RetInt(s[o + 5]) - (RetInt(s[o + 6]) ^ (RetInt(s[o + 7]) - (RetInt(s[o + 8]) ^ (RetInt(s[o + 9])
                - (RetInt(s[o + 10]) ^ (RetInt(s[o + 11]) - (RetInt(s[o + 12]) ^ RetInt(s[o + 13])))))))))))));
        long y = s[o + 12] ^ s[o + 13];
        for (int k = 11; k >= 0; k--)
            y = (k & 1) == 0 ? s[o + k] ^ y : s[o + k] - y;
        if (x != Sx(y, 32)) bad |= 2;

        ulong z = (ulong)RetUInt(s[o]) + ((ulong)RetUInt(s[o + 1]) + ((ulong)RetUInt(s[o + 2]) + ((ulong)RetUInt(s[o + 3])
                  + ((ulong)RetUInt(s[o + 4]) + ((ulong)RetUInt(s[o + 5]) + ((ulong)RetUInt(s[o + 6])
                  + ((ulong)RetUInt(s[o + 7]) + ((ulong)RetUInt(s[o + 8]) + ((ulong)RetUInt(s[o + 9])
                  + ((ulong)RetUInt(s[o + 10]) + ((ulong)RetUInt(s[o + 11]) + (ulong)RetUInt(s[o + 12]))))))))))));
        ulong wantZ = 0;
        for (int k = 0; k < 13; k++)
            wantZ += (ulong)Zx(s[o + k], 32);
        if (z != wantZ) bad |= 4;
        return bad;
    }

    // ---- compares of values from homes ----

    // The locals are read by the handler, so every compare below works on
    // values that came back from their homes.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Compare(long ra, long rb, long z, int code)
    {
        int ia = (int)(ra + z), ib = (int)RetUInt(rb);
        uint ua = (uint)((ulong)ra >> (int)z), ub = RetUInt(rb);
        int m = 0;
        try
        {
            Thrower(code);
        }
        catch (E)
        {
            if (ia == (int)ua) m |= 1 << 10;
        }
        if (ia < ib) m |= 1 << 0;
        if (ua < ub) m |= 1 << 1;
        if (ia == ib) m |= 1 << 2;
        if (ua == ub) m |= 1 << 3;
        if ((uint)ia == ub) m |= 1 << 4;
        if (ia == (int)ua) m |= 1 << 5;
        if (ia < (long)ub) m |= 1 << 6;
        if (ua > (ulong)(long)ib) m |= 1 << 7;
        if (ia <= (int)ub) m |= 1 << 8;
        if ((uint)ib >= ua) m |= 1 << 9;
        return m;
    }

    static int CompareWant(long ra, long rb, int code)
    {
        long sa = Sx(ra, 32), sb = Sx(rb, 32), za = Zx(ra, 32), zb = Zx(rb, 32);
        int m = 1 << 5;
        if (code > 0) m |= 1 << 10;
        if (sa < sb) m |= 1 << 0;
        if (za < zb) m |= 1 << 1;
        if (sa == sb) m |= 1 << 2;
        if (za == zb) m |= 1 << 3;
        if (za == zb) m |= 1 << 4;
        if (sa < zb) m |= 1 << 6;
        if ((ulong)za > (ulong)sb) m |= 1 << 7;
        if (sa <= sb) m |= 1 << 8;
        if (zb >= za) m |= 1 << 9;
        return m;
    }

    // ---- driver ----

    static void SetQ(int k)
    {
        int n = R.Length;
        q0 = R[k % n]; q1 = R[(k + 1) % n]; q2 = R[(k + 2) % n]; q3 = R[(k + 3) % n];
        q4 = R[(k + 4) % n]; q5 = R[(k + 5) % n]; q6 = R[(k + 6) % n]; q7 = R[(k + 7) % n];
    }

    static int Main()
    {
        int n = R.Length;
        long[] s = new long[n + 16];
        uint[] lo = new uint[n + 16];
        for (int i = 0; i < s.Length; i++)
        {
            s[i] = R[i % n];
            lo[i] = (uint)R[i % n];
        }

        int bad = 0, badEH = 0, badNoOpt = 0;
        for (int o = 0; o < n; o++)
        {
            Dirty();
            bad |= IntHomes(s, lo, o, 0);
            Dirty();
            badEH |= IntHomesEH(s, lo, o, 0, 0) | IntHomesEH(s, lo, o, 0, o + 1);
            Dirty();
            badNoOpt |= IntHomesNoOpt(s, lo, o, 0);
        }
        Check("int locals, spilled", bad);
        Check("int locals, handler", badEH);
        Check("int locals, no optimization", badNoOpt);

        bad = 0; badNoOpt = 0;
        for (int o = 0; o < n; o++)
        {
            Dirty();
            bad |= SmallHomes(s, o, 0, 0) | SmallHomes(s, o, 0, o + 1);
            Dirty();
            badNoOpt |= SmallHomesNoOpt(s, o, 0);
        }
        Check("small locals, handler", bad);
        Check("small locals, no optimization", badNoOpt);

        bad = 0; badEH = 0; badNoOpt = 0;
        int badLarge = 0, badLargeSpill = 0;
        for (int k = 0; k < n; k++)
        {
            SetQ(k);
            int a = (int)q0;
            uint b = (uint)q1;
            short c = (short)q2;
            ushort d = (ushort)q3;
            byte e = (byte)q4;
            sbyte f = (sbyte)q5;
            char g = (char)q6;
            bool h = (q7 & 1) != 0;
            Dirty();
            bad |= ParamHomes(a, b, c, d, e, f, g, h);
            Dirty();
            badEH |= ParamHomesEH(a, b, c, d, e, f, g, h, 0) | ParamHomesEH(a, b, c, d, e, f, g, h, k + 1);
            Dirty();
            badNoOpt |= ParamHomesNoOpt(a, b, c, d, e, f, g, h);
            Dirty();
            badLarge |= LargeFrame(a, b, c, d, e, f, g, h, 0) | LargeFrame(a, b, c, d, e, f, g, h, k + 1);
            Dirty();
            badLargeSpill |= LargeSpill(a, b, c, d, e, f, g, h);
        }
        Check("parameters, spilled", bad);
        Check("parameters, handler", badEH);
        Check("parameters, no optimization", badNoOpt);
        Check("large frame, handler", badLarge);
        Check("large frame, spilled", badLargeSpill);

        // Small parameters with bits above their width, as a caller that
        // does not narrow them leaves them: the callee has to narrow.
        bad = 0; badEH = 0; badNoOpt = 0; badLarge = 0;
        delegate*<int, uint, short, ushort, byte, sbyte, char, bool, int> f1 = &ParamHomes, f2 = &ParamHomesNoOpt;
        delegate*<int, uint, short, ushort, byte, sbyte, char, bool, int, int> f3 = &ParamHomesEH, f4 = &LargeFrame;
        var wide = (delegate*<int, uint, int, int, int, int, int, int, int>)(void*)f1;
        var wideNoOpt = (delegate*<int, uint, int, int, int, int, int, int, int>)(void*)f2;
        var wideEH = (delegate*<int, uint, int, int, int, int, int, int, int, int>)(void*)f3;
        var wideLarge = (delegate*<int, uint, int, int, int, int, int, int, int, int>)(void*)f4;
        for (int k = 0; k < n; k++)
        {
            SetQ(k);
            int h = (int)(q7 & 1);
            Dirty();
            bad |= wide((int)q0, (uint)q1, (int)q2, (int)q3, (int)q4, (int)q5, (int)q6, h);
            badNoOpt |= wideNoOpt((int)q0, (uint)q1, (int)q2, (int)q3, (int)q4, (int)q5, (int)q6, h);
            Dirty();
            badEH |= wideEH((int)q0, (uint)q1, (int)q2, (int)q3, (int)q4, (int)q5, (int)q6, h, 0);
            badEH |= wideEH((int)q0, (uint)q1, (int)q2, (int)q3, (int)q4, (int)q5, (int)q6, h, k + 1);
            Dirty();
            badLarge |= wideLarge((int)q0, (uint)q1, (int)q2, (int)q3, (int)q4, (int)q5, (int)q6, h, k + 1);
        }
        Check("wide small parameters, spilled", bad);
        Check("wide small parameters, handler", badEH);
        Check("wide small parameters, no optimization", badNoOpt);
        Check("wide small parameters, large frame", badLarge);

        Dirty();
        Check("stack parameters", StackParams(1, 2, 3, 4, 5, 6, 7, 8, 0x11111111, 0x22222222, 0x3333, 0x44, 0x55555555, 0));

        bad = 0;
        for (int code = 0; code < 2; code++)
        {
            P2[] ps = { new P2 { X = 0x11111111, Y = 0x22222222 }, default };
            S4[] qs = { new S4 { A = 0x33333333, B = 0x4444, C = 0x55, D = true }, default };
            Dirty();
            bad |= StructHomes(ps[0], qs[0], ps, qs, code, 0);
        }
        Check("struct fields", bad);
        Dirty();
        Check("struct in memory", MemoryStruct(0) | MemoryStructNoOpt(0) << 8);

        Dirty();
        Check("address-taken, no optimization", AddressTakenNoOpt(0));
        Dirty();
        Check("address-taken", AddressTaken(0, 0) | AddressTaken(0, 3) << 8);
        Dirty();
        Check("partial access", Partial(0) | PartialNoOpt(0) << 8);

        bad = 0;
        int badCmp = 0;
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                bad |= Temps(R[i], R[j], R[(i + j) % n]);
                int code = (i + j) & 1;
                if (Compare(R[i], R[j], 0, code) != CompareWant(R[i], R[j], code)) badCmp |= 1 << (i % 24);
            }
        }
        Check("spill temps", bad);
        bad = 0;
        for (int o = 0; o < n; o++)
            bad |= ManyTemps(s, o);
        Check("many spill temps", bad);
        Check("compares", badCmp);

        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails;
    }
}
