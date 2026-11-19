// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// 64-bit shifts and rotates on a target whose registers hold 32 bits
// (riscv32).
//
// A long is two int halves there. A shift by a constant is rewritten by its
// count: 0 is a copy, 1..31 moves bits across the halves with one combined
// node per half ((hi << c) | (lo >> (32 - c)) and its mirror), 32 moves a
// half whole, 33..63 shifts the one half that survives and fills the other
// with zero or with the sign. A shift by a variable count is branch-free:
//
//   mask  = count & 32 ? -1 : 0
//   outer = the half the bits leave, shifted alone by count & 31
//   inner = the half they enter, plus the bits that cross:
//           (x >> 1) >> ~count stands for x >> (32 - count), zero when count is a multiple of 32
//   left:   (lo, hi) = mask ? (0, outer)    : (outer, inner)
//   right:  (lo, hi) = mask ? (outer, fill) : (inner, outer), fill = 0 or hi >> 31
//
// What can go wrong: the count 0 or 32 where nothing or everything crosses,
// 31 and 33 next to it, 63, a count the caller did not mask (C# masks a long
// shift count with 63, and the mask can be dropped as redundant), a logical
// shift where an arithmetic one is needed and the other way round in either
// half, the fill of the high half for a negative value, the half that was
// overwritten before the other was computed from it when the result reuses
// the operand's registers, and a temp of one shift reused by the next one in
// the same expression.
//
// Every value of the table is shifted left, right logical and right
// arithmetic by every count 0..63 as a variable and by 22 counts as a
// constant; counts outside 0..63 and counts of other types are used too.
// Rotates, bit tests, masks, byte extraction and assembly, loops that shift
// an accumulator, shifts on values in memory and the bit counting methods of
// BitOperations follow.
//
// The expected value is computed bit by bit on 16-bit limbs read from memory
// with int operations only (class R), and results are compared half by half
// through memory, so neither uses a long shift.
//
// Each group prints the number of checks; a failed check prints one line
// with the operand, the count, the expected and the actual value. The run
// ends with ALL PASS or a FAIL count, and the guest returns 1 on failure.
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

// Reference 64-bit arithmetic on 16-bit limbs, int operations only.
unsafe static class R
{
    const MethodImplOptions NI = MethodImplOptions.NoInlining;

    [MethodImpl(NI)] public static uint L(long x, int i) => ((ushort*)&x)[i];
    [MethodImpl(NI)] public static uint Lo(long x) => ((uint*)&x)[0];
    [MethodImpl(NI)] public static uint Hi(long x) => ((uint*)&x)[1];

    [MethodImpl(NI)]
    public static long Make(uint l0, uint l1, uint l2, uint l3)
    {
        long r = 0;
        ushort* p = (ushort*)&r;
        p[0] = (ushort)l0; p[1] = (ushort)l1; p[2] = (ushort)l2; p[3] = (ushort)l3;
        return r;
    }

    public static long C(uint hi, uint lo) => Make(lo & 0xFFFF, lo >> 16, hi & 0xFFFF, hi >> 16);

    [MethodImpl(NI)] public static bool Same(long a, long b) => Lo(a) == Lo(b) && Hi(a) == Hi(b);

    public static string Hex(long x) => "0x" + Hi(x).ToString("x8") + Lo(x).ToString("x8");

    public static uint Bit(long a, int i) => (L(a, i >> 4) >> (i & 15)) & 1;

    // Bit j of the result is bit j + by of a; bits from outside 0..63 are `fill`.
    [MethodImpl(NI)]
    static long Move(long a, int by, uint fill)
    {
        uint* r = stackalloc uint[4];
        r[0] = 0; r[1] = 0; r[2] = 0; r[3] = 0;
        for (int j = 0; j < 64; j++)
        {
            int src = j + by;
            uint bit = (src >= 0 && src < 64) ? Bit(a, src) : fill;
            if (src < 0) bit = 0;
            r[j >> 4] |= bit << (j & 15);
        }
        return Make(r[0], r[1], r[2], r[3]);
    }

    public static long Shl(long a, int n) => Move(a, -(n & 63), 0);
    public static long Shr(long a, int n) => Move(a, n & 63, 0);
    public static long Sar(long a, int n) => Move(a, n & 63, Bit(a, 63));
    public static long Rol(long a, int n) => Or(Shl(a, n), (n & 63) == 0 ? C(0, 0) : Shr(a, 64 - (n & 63)));
    public static long Ror(long a, int n) => Or(Shr(a, n), (n & 63) == 0 ? C(0, 0) : Shl(a, 64 - (n & 63)));

    public static long Or(long a, long b) => Make(L(a, 0) | L(b, 0), L(a, 1) | L(b, 1), L(a, 2) | L(b, 2), L(a, 3) | L(b, 3));
    public static long And(long a, long b) => Make(L(a, 0) & L(b, 0), L(a, 1) & L(b, 1), L(a, 2) & L(b, 2), L(a, 3) & L(b, 3));
    public static long Xor(long a, long b) => Make(L(a, 0) ^ L(b, 0), L(a, 1) ^ L(b, 1), L(a, 2) ^ L(b, 2), L(a, 3) ^ L(b, 3));

    public static int PopCount(long a) { int n = 0; for (int i = 0; i < 64; i++) n += (int)Bit(a, i); return n; }
    public static int Lzc(long a) { int n = 0; for (int i = 63; i >= 0 && Bit(a, i) == 0; i--) n++; return n; }
    public static int Tzc(long a) { int n = 0; for (int i = 0; i < 64 && Bit(a, i) == 0; i++) n++; return n; }
}

unsafe class Program
{
    const MethodImplOptions NI = MethodImplOptions.NoInlining;

    static int fails;
    static int checks;

    static void Fail(string name, long a, int n, string exp, string got)
    {
        fails++;
        if (fails <= 60)
            Console.WriteLine("FAIL " + name + " a=" + R.Hex(a) + " n=" + n.ToString() + " expected " + exp + " got " + got);
    }

    static void Check(string name, long a, int n, long got, long exp)
    {
        checks++;
        if (!R.Same(got, exp)) Fail(name, a, n, R.Hex(exp), R.Hex(got));
    }

    static void CheckI(string name, long a, int n, int got, int exp)
    {
        checks++;
        if (got != exp) Fail(name, a, n, exp.ToString(), got.ToString());
    }

    static void Group(string name)
    {
        Console.WriteLine("ok   " + name + " " + checks.ToString());
    }

    static readonly long[] V =
    {
        0L, 1L, -1L, 2L, 3L,
        0x7FFFFFFFL, 0x80000000L, 0xFFFFFFFFL, 0x100000000L, 0x100000001L,
        -0x80000000L, -0x100000000L,
        0x7FFFFFFFFFFFFFFFL, unchecked((long)0x8000000000000000UL), unchecked((long)0x8000000000000001UL),
        unchecked((long)0x800000007FFFFFFFUL), 0x7FFFFFFF80000000L, unchecked((long)0xC000000000000003UL),
        0x0123456789ABCDEFL, unchecked((long)0xFEDCBA9876543210UL), 0x5555555555555555L,
        unchecked((long)0xAAAAAAAAAAAAAAAAUL), 0x00000001FFFFFFFEL, unchecked((long)0xFFFFFFFE00000001UL),
    };

    // ---- a variable count ----

    [MethodImpl(NI)] static long Shl(long a, int n) => a << n;
    [MethodImpl(NI)] static long Sar(long a, int n) => a >> n;
    [MethodImpl(NI)] static ulong Shr(ulong a, int n) => a >> n;
    [MethodImpl(NI)] static long ShrL(long a, int n) => a >>> n;
    [MethodImpl(NI)] static ulong ShlU(ulong a, int n) => a << n;

    // The count comes first, so the value sits in other registers.
    [MethodImpl(NI)] static long ShlN(int n, long a) => a << n;
    [MethodImpl(NI)] static long SarN(int n, long a) => a >> n;
    [MethodImpl(NI)] static ulong ShrN(int n, ulong a) => a >> n;

    // The count in other types and from memory.
    [MethodImpl(NI)] static long ShlByte(long a, byte n) => a << n;
    [MethodImpl(NI)] static long SarSByte(long a, sbyte n) => a >> n;
    [MethodImpl(NI)] static ulong ShrLong(ulong a, long n) => a >> (int)n;
    [MethodImpl(NI)] static long ShlMem(long a, int[] n) => a << n[0];
    [MethodImpl(NI)] static long SarMem(long[] a, int[] n) => a[0] >> n[0];
    [MethodImpl(NI)] static void ShrStore(ulong[] a, int n) { a[1] = a[0] >> n; a[0] <<= n; }

    // Counts built in the method.
    [MethodImpl(NI)] static long ShlAnd31(long a, int n) => a << (n & 31);
    [MethodImpl(NI)] static long ShlOr32(long a, int n) => a << (n | 32);
    [MethodImpl(NI)] static long ShlPlus32(long a, int n) => a << (n + 32);
    [MethodImpl(NI)] static long SarAnd31(long a, int n) => a >> (n & 31);
    [MethodImpl(NI)] static long SarOr32(long a, int n) => a >> (n | 32);
    [MethodImpl(NI)] static ulong Shr64Minus(ulong a, int n) => a >> (64 - n);
    [MethodImpl(NI)] static long ShlNeg(long a, int n) => a << -n;

    // Two and three shifts of one value: the temps of one must not serve the next.
    [MethodImpl(NI)] static ulong RolManual(ulong a, int n) => (a << n) | (a >> (64 - n));
    [MethodImpl(NI)] static ulong RorManual(ulong a, int n) => (a >> n) | (a << (64 - n));
    [MethodImpl(NI)] static ulong Rol(ulong a, int n) => BitOperations.RotateLeft(a, n);
    [MethodImpl(NI)] static ulong Ror(ulong a, int n) => BitOperations.RotateRight(a, n);
    [MethodImpl(NI)] static long Three(long a, int n) => (a << n) ^ (a >> n) ^ (long)((ulong)a >> n);
    [MethodImpl(NI)] static long TwoCounts(long a, int n, int m) => (a << n) + (a >> m);
    [MethodImpl(NI)] static long ShiftBack(long a, int n) => (a << n) >> n;
    [MethodImpl(NI)] static ulong ShiftBackU(ulong a, int n) => (a >> n) << n;

    // Bit tests and masks.
    [MethodImpl(NI)] static long OneShl(int n) => 1L << n;
    [MethodImpl(NI)] static ulong MaskBelow(int n) => (1UL << n) - 1;
    [MethodImpl(NI)] static bool TestBit(long a, int n) => (a & (1L << n)) != 0;
    [MethodImpl(NI)] static int GetBit(long a, int n) => (int)((a >> n) & 1);
    [MethodImpl(NI)] static long SetBit(long a, int n) => a | (1L << n);
    [MethodImpl(NI)] static long ClearBit(long a, int n) => a & ~(1L << n);
    [MethodImpl(NI)] static long MinusOneShl(int n) => -1L << n;
    [MethodImpl(NI)] static ulong MinusOneShr(int n) => ulong.MaxValue >> n;

    // Loops that shift an accumulator.
    [MethodImpl(NI)]
    static long ShlLoop(long a, int times)
    {
        for (int i = 0; i < times; i++) a <<= 1;
        return a;
    }

    [MethodImpl(NI)]
    static int CountBitsLoop(ulong a)
    {
        int n = 0;
        while (a != 0) { n += (int)(a & 1); a >>= 1; }
        return n;
    }

    [MethodImpl(NI)]
    static long SarLoopGrowing(long a, int times)
    {
        long acc = 0;
        for (int i = 0; i < times; i++) acc ^= a >> i;
        return acc;
    }

    [MethodImpl(NI)]
    static ulong FromBytes(byte* p)
    {
        ulong r = 0;
        for (int i = 0; i < 8; i++) r |= (ulong)p[i] << (8 * i);
        return r;
    }

    [MethodImpl(NI)]
    static void ToBytes(ulong a, byte* p)
    {
        for (int i = 0; i < 8; i++) p[i] = (byte)(a >> (8 * i));
    }

    [MethodImpl(NI)] static ulong FromBytesBE(byte* p) =>
        ((ulong)p[0] << 56) | ((ulong)p[1] << 48) | ((ulong)p[2] << 40) | ((ulong)p[3] << 32) |
        ((ulong)p[4] << 24) | ((ulong)p[5] << 16) | ((ulong)p[6] << 8) | p[7];

    [MethodImpl(NI)] static ulong Combine(uint hi, uint lo) => ((ulong)hi << 32) | lo;
    [MethodImpl(NI)] static long CombineS(int hi, int lo) => ((long)hi << 32) | (uint)lo;
    [MethodImpl(NI)] static uint HighHalf(ulong a) => (uint)(a >> 32);
    [MethodImpl(NI)] static int HighHalfS(long a) => (int)(a >> 32);
    [MethodImpl(NI)] static ulong Bswap(ulong a) => System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(a);

    [MethodImpl(NI)] static int Pop(ulong a) => BitOperations.PopCount(a);
    [MethodImpl(NI)] static int Lzc(ulong a) => BitOperations.LeadingZeroCount(a);
    [MethodImpl(NI)] static int Tzc(ulong a) => BitOperations.TrailingZeroCount(a);
    [MethodImpl(NI)] static int Log2(ulong a) => BitOperations.Log2(a);

    // ---- a constant count ----

    delegate long Op1(long a);

    static readonly int[] ConstCounts = { 0, 1, 2, 3, 7, 8, 12, 15, 16, 24, 30, 31, 32, 33, 34, 40, 47, 48, 56, 61, 62, 63 };

    static readonly Op1[] ShlConst =
    {
        a => a << 0, a => a << 1, a => a << 2, a => a << 3, a => a << 7, a => a << 8, a => a << 12, a => a << 15,
        a => a << 16, a => a << 24, a => a << 30, a => a << 31, a => a << 32, a => a << 33, a => a << 34,
        a => a << 40, a => a << 47, a => a << 48, a => a << 56, a => a << 61, a => a << 62, a => a << 63,
    };

    static readonly Op1[] SarConst =
    {
        a => a >> 0, a => a >> 1, a => a >> 2, a => a >> 3, a => a >> 7, a => a >> 8, a => a >> 12, a => a >> 15,
        a => a >> 16, a => a >> 24, a => a >> 30, a => a >> 31, a => a >> 32, a => a >> 33, a => a >> 34,
        a => a >> 40, a => a >> 47, a => a >> 48, a => a >> 56, a => a >> 61, a => a >> 62, a => a >> 63,
    };

    static readonly Op1[] ShrConst =
    {
        a => a >>> 0, a => a >>> 1, a => a >>> 2, a => a >>> 3, a => a >>> 7, a => a >>> 8, a => a >>> 12, a => a >>> 15,
        a => a >>> 16, a => a >>> 24, a => a >>> 30, a => a >>> 31, a => a >>> 32, a => a >>> 33, a => a >>> 34,
        a => a >>> 40, a => a >>> 47, a => a >>> 48, a => a >>> 56, a => a >>> 61, a => a >>> 62, a => a >>> 63,
    };

    // Constant counts in methods called directly: the value arrives in a0/a1.
    [MethodImpl(NI)] static long Shl1(long a) => a << 1;
    [MethodImpl(NI)] static long Shl31(long a) => a << 31;
    [MethodImpl(NI)] static long Shl32(long a) => a << 32;
    [MethodImpl(NI)] static long Shl33(long a) => a << 33;
    [MethodImpl(NI)] static long Sar1(long a) => a >> 1;
    [MethodImpl(NI)] static long Sar31(long a) => a >> 31;
    [MethodImpl(NI)] static long Sar32(long a) => a >> 32;
    [MethodImpl(NI)] static long Sar33(long a) => a >> 33;
    [MethodImpl(NI)] static long Sar63(long a) => a >> 63;
    [MethodImpl(NI)] static ulong Shr1(ulong a) => a >> 1;
    [MethodImpl(NI)] static ulong Shr31(ulong a) => a >> 31;
    [MethodImpl(NI)] static ulong Shr32(ulong a) => a >> 32;
    [MethodImpl(NI)] static ulong Shr33(ulong a) => a >> 33;
    [MethodImpl(NI)] static ulong Shr63(ulong a) => a >> 63;
    [MethodImpl(NI)] static ulong Rol13(ulong a) => (a << 13) | (a >> 51);
    [MethodImpl(NI)] static ulong Rol32(ulong a) => BitOperations.RotateLeft(a, 32);
    [MethodImpl(NI)] static ulong Ror7(ulong a) => BitOperations.RotateRight(a, 7);
    [MethodImpl(NI)] static ulong Ror45(ulong a) => BitOperations.RotateRight(a, 45);
    [MethodImpl(NI)] static long ShlAdd(long a, long b) => (a << 3) + (b << 35) - (a >> 5);
    [MethodImpl(NI)] static long Times8Plus(long a) => a * 8 + a;
    [MethodImpl(NI)] static ulong Xorshift(ulong x) { x ^= x << 13; x ^= x >> 7; x ^= x << 17; return x; }

    static void TestVariable()
    {
        int[] cell = new int[1];
        long[] vcell = new long[1];
        ulong[] ucell = new ulong[2];
        foreach (long a in V)
        {
            for (int n = 0; n < 64; n++)
            {
                long shl = R.Shl(a, n), shr = R.Shr(a, n), sar = R.Sar(a, n);
                Check("shl", a, n, Shl(a, n), shl);
                Check("sar", a, n, Sar(a, n), sar);
                Check("shr", a, n, (long)Shr((ulong)a, n), shr);
                Check("shr long", a, n, ShrL(a, n), shr);
                Check("shl ulong", a, n, (long)ShlU((ulong)a, n), shl);
                Check("shl count first", a, n, ShlN(n, a), shl);
                Check("sar count first", a, n, SarN(n, a), sar);
                Check("shr count first", a, n, (long)ShrN(n, (ulong)a), shr);
                Check("shl byte count", a, n, ShlByte(a, (byte)n), shl);
                Check("sar sbyte count", a, n, SarSByte(a, (sbyte)n), sar);
                Check("shr long count", a, n, (long)ShrLong((ulong)a, n), shr);
                cell[0] = n; vcell[0] = a; ucell[0] = (ulong)a;
                Check("shl count in memory", a, n, ShlMem(a, cell), shl);
                Check("sar all in memory", a, n, SarMem(vcell, cell), sar);
                ShrStore(ucell, n);
                Check("shr stored", a, n, (long)ucell[1], shr);
                Check("shl stored", a, n, (long)ucell[0], shl);

                Check("shl n & 31", a, n, ShlAnd31(a, n), R.Shl(a, n & 31));
                Check("shl n | 32", a, n, ShlOr32(a, n), R.Shl(a, n | 32));
                Check("shl n + 32", a, n, ShlPlus32(a, n), R.Shl(a, (n + 32) & 63));
                Check("sar n & 31", a, n, SarAnd31(a, n), R.Sar(a, n & 31));
                Check("sar n | 32", a, n, SarOr32(a, n), R.Sar(a, n | 32));
                Check("shr 64 - n", a, n, (long)Shr64Minus((ulong)a, n), R.Shr(a, (64 - n) & 63));
                Check("shl -n", a, n, ShlNeg(a, n), R.Shl(a, (-n) & 63));

                Check("rol manual", a, n, (long)RolManual((ulong)a, n), R.Rol(a, n));
                Check("ror manual", a, n, (long)RorManual((ulong)a, n), R.Ror(a, n));
                Check("rol", a, n, (long)Rol((ulong)a, n), R.Rol(a, n));
                Check("ror", a, n, (long)Ror((ulong)a, n), R.Ror(a, n));
                Check("three shifts", a, n, Three(a, n), R.Xor(R.Xor(shl, sar), shr));
                Check("shift back", a, n, ShiftBack(a, n), R.Sar(shl, n));
                Check("shift back u", a, n, (long)ShiftBackU((ulong)a, n), R.Shl(shr, n));

                CheckI("test bit", a, n, TestBit(a, n) ? 1 : 0, (int)R.Bit(a, n));
                CheckI("get bit", a, n, GetBit(a, n), (int)R.Bit(a, n));
                Check("set bit", a, n, SetBit(a, n), R.Or(a, R.Shl(R.C(0, 1), n)));
                Check("clear bit", a, n, ClearBit(a, n), R.And(a, R.Xor(R.Shl(R.C(0, 1), n), R.C(0xFFFFFFFF, 0xFFFFFFFF))));
            }

            // Counts the caller does not mask: only the low six bits count.
            int[] wild = { 64, 65, 95, 96, 127, 128, -1, -31, -32, -33, -64, 0x7FFFFFFF, -0x80000000, 1000 };
            foreach (int n in wild)
            {
                Check("shl wild", a, n, Shl(a, n), R.Shl(a, n));
                Check("sar wild", a, n, Sar(a, n), R.Sar(a, n));
                Check("shr wild", a, n, (long)Shr((ulong)a, n), R.Shr(a, n));
                Check("rol wild", a, n, (long)Rol((ulong)a, n), R.Rol(a, n));
            }

            for (int n = 0; n < 64; n += 5)
            {
                for (int m = 0; m < 64; m += 7)
                {
                    long exp = (long)((ulong)R.Shl(a, n));
                    // The sum is taken by halves to stay off the long add as well.
                    long sarm = R.Sar(a, m);
                    uint lo = R.Lo(exp) + R.Lo(sarm);
                    uint carry = lo < R.Lo(sarm) ? 1u : 0u;
                    uint hi = R.Hi(exp) + R.Hi(sarm) + carry;
                    Check("two counts", a, n * 100 + m, TwoCounts(a, n, m), R.C(hi, lo));
                }
            }
        }

        for (int n = 0; n < 64; n++)
        {
            Check("1 << n", 1, n, OneShl(n), R.Shl(R.C(0, 1), n));
            Check("-1 << n", -1, n, MinusOneShl(n), R.Shl(R.C(0xFFFFFFFF, 0xFFFFFFFF), n));
            Check("max >> n", -1, n, (long)MinusOneShr(n), R.Shr(R.C(0xFFFFFFFF, 0xFFFFFFFF), n));
            long below = R.Shr(R.C(0xFFFFFFFF, 0xFFFFFFFF), 64 - n);
            if (n == 0) below = R.C(0, 0);
            Check("mask below", 1, n, (long)MaskBelow(n), below);
        }
        Group("variable count");
    }

    static void TestConstant()
    {
        foreach (long a in V)
        {
            for (int i = 0; i < ConstCounts.Length; i++)
            {
                int n = ConstCounts[i];
                Check("shl const", a, n, ShlConst[i](a), R.Shl(a, n));
                Check("sar const", a, n, SarConst[i](a), R.Sar(a, n));
                Check("shr const", a, n, ShrConst[i](a), R.Shr(a, n));
            }

            Check("shl 1", a, 1, Shl1(a), R.Shl(a, 1));
            Check("shl 31", a, 31, Shl31(a), R.Shl(a, 31));
            Check("shl 32", a, 32, Shl32(a), R.Shl(a, 32));
            Check("shl 33", a, 33, Shl33(a), R.Shl(a, 33));
            Check("sar 1", a, 1, Sar1(a), R.Sar(a, 1));
            Check("sar 31", a, 31, Sar31(a), R.Sar(a, 31));
            Check("sar 32", a, 32, Sar32(a), R.Sar(a, 32));
            Check("sar 33", a, 33, Sar33(a), R.Sar(a, 33));
            Check("sar 63", a, 63, Sar63(a), R.Sar(a, 63));
            Check("shr 1", a, 1, (long)Shr1((ulong)a), R.Shr(a, 1));
            Check("shr 31", a, 31, (long)Shr31((ulong)a), R.Shr(a, 31));
            Check("shr 32", a, 32, (long)Shr32((ulong)a), R.Shr(a, 32));
            Check("shr 33", a, 33, (long)Shr33((ulong)a), R.Shr(a, 33));
            Check("shr 63", a, 63, (long)Shr63((ulong)a), R.Shr(a, 63));
            Check("rol 13", a, 13, (long)Rol13((ulong)a), R.Rol(a, 13));
            Check("rol 32", a, 32, (long)Rol32((ulong)a), R.Rol(a, 32));
            Check("ror 7", a, 7, (long)Ror7((ulong)a), R.Ror(a, 7));
            Check("ror 45", a, 45, (long)Ror45((ulong)a), R.Ror(a, 45));

            long x = a;
            x = R.Xor(x, R.Shl(x, 13));
            x = R.Xor(x, R.Shr(x, 7));
            x = R.Xor(x, R.Shl(x, 17));
            Check("xorshift", a, 0, (long)Xorshift((ulong)a), x);

            Check("combine", a, 0, (long)Combine(R.Hi(a), R.Lo(a)), a);
            Check("combine signed", a, 0, CombineS((int)R.Hi(a), (int)R.Lo(a)), a);
            CheckI("high half", a, 0, (int)HighHalf((ulong)a), (int)R.Hi(a));
            CheckI("high half signed", a, 0, HighHalfS(a), (int)R.Hi(a));
        }
        Group("constant count");
    }

    static void TestLoopsAndBytes()
    {
        byte* bytes = stackalloc byte[8];
        byte* back = stackalloc byte[8];
        foreach (long a in V)
        {
            for (int times = 0; times <= 64; times += 4)
                Check("shl loop", a, times, ShlLoop(a, times), times == 64 ? R.C(0, 0) : R.Shl(a, times));

            CheckI("count bits loop", a, 0, CountBitsLoop((ulong)a), R.PopCount(a));

            long acc = R.C(0, 0);
            for (int i = 0; i < 40; i++) acc = R.Xor(acc, R.Sar(a, i));
            Check("sar loop", a, 40, SarLoopGrowing(a, 40), acc);

            for (int i = 0; i < 8; i++) bytes[i] = (byte)(R.L(a, i >> 1) >> ((i & 1) * 8));
            Check("from bytes", a, 0, (long)FromBytes(bytes), a);
            ToBytes((ulong)a, back);
            for (int i = 0; i < 8; i++) CheckI("to bytes", a, i, back[i], bytes[i]);

            long swapped = R.Make(
                ((R.L(a, 3) >> 8) | (R.L(a, 3) << 8)) & 0xFFFF, ((R.L(a, 2) >> 8) | (R.L(a, 2) << 8)) & 0xFFFF,
                ((R.L(a, 1) >> 8) | (R.L(a, 1) << 8)) & 0xFFFF, ((R.L(a, 0) >> 8) | (R.L(a, 0) << 8)) & 0xFFFF);
            Check("from bytes big endian", a, 0, (long)FromBytesBE(bytes), swapped);
            Check("reverse endianness", a, 0, (long)Bswap((ulong)a), swapped);

            CheckI("popcount", a, 0, Pop((ulong)a), R.PopCount(a));
            CheckI("leading zeros", a, 0, Lzc((ulong)a), R.Lzc(a));
            CheckI("trailing zeros", a, 0, Tzc((ulong)a), R.Tzc(a));
            CheckI("log2", a, 0, Log2((ulong)a), R.Lzc(a) == 64 ? 0 : 63 - R.Lzc(a));

            foreach (long b in V)
            {
                // (a << 3) + (b << 35) - (a >> 5), the sums by halves.
                long t1 = R.Shl(a, 3), t2 = R.Shl(b, 35), t3 = R.Sar(a, 5);
                uint lo = R.Lo(t1) + R.Lo(t2);
                uint hi = R.Hi(t1) + R.Hi(t2) + (lo < R.Lo(t2) ? 1u : 0u);
                uint lo2 = lo - R.Lo(t3);
                uint hi2 = hi - R.Hi(t3) - (lo < R.Lo(t3) ? 1u : 0u);
                Check("shifts and adds", a, 0, ShlAdd(a, b), R.C(hi2, lo2));
            }

            {
                long t = R.Shl(a, 3);
                uint lo = R.Lo(t) + R.Lo(a);
                uint hi = R.Hi(t) + R.Hi(a) + (lo < R.Lo(a) ? 1u : 0u);
                Check("times 8 plus", a, 0, Times8Plus(a), R.C(hi, lo));
            }
        }
        Group("loops and bytes");
    }

    static int Main()
    {
        TestVariable();
        TestConstant();
        TestLoopsAndBytes();

        if (fails == 0)
        {
            Console.WriteLine("ALL PASS");
            return 0;
        }
        Console.WriteLine("FAIL " + fails.ToString());
        return 1;
    }
}
