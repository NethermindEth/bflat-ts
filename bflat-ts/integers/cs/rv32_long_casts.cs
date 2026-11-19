// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Conversions between 64-bit and narrower integers, and 32x32->64-bit
// multiplication, on a target whose registers hold 32 bits (riscv32).
//
// A long is two int halves there. Widening builds the high half: zero for an
// unsigned source, the low half shifted right by 31 for a signed one, and
// for a small type only after the low half itself was extended from 8 or 16
// bits. Narrowing takes the low half; a checked narrowing tests the high
// half first:
//
//   long  -> int:   hi == lo >> 31
//   ulong -> int:   hi == 0 && lo >= 0
//   [u]long -> uint: hi == 0
//   long <-> ulong: hi >= 0
//
// and a checked narrowing to a small type is one of these followed by the
// int check. A native int is 32 bits, so nint, nuint and pointers convert
// like int and uint. The product of two ints widened to long is one mul for
// the low half and one mulh or mulhu for the high half - but only when both
// factors are widened the same way and only for that shape; a product with a
// factor that is already a long is the full three-multiply form.
//
// What can go wrong: a high half that is zero where it has to be the sign or
// the other way round, a sign taken from a small value before it was
// extended, a checked conversion that tests only one half or tests the low
// half with the wrong signedness, a high half that is left unevaluated or is
// evaluated twice when the conversion throws its value away, mulh used for
// unsigned factors or mulhu for signed ones, the widening multiply shape
// applied to a product it does not fit.
//
// Every conversion is a method that is not inlined, applied to values on the
// limits of 8, 16, 32 and 64 bits. The expected halves are computed with int
// operations (class R holds the 16-bit limb arithmetic for the products),
// and results are compared half by half through memory.
//
// Each group prints the number of checks; a failed check prints one line
// with the operands, the expected and the actual value. The run ends with
// ALL PASS or a FAIL count, and the guest returns 1 on failure.
using System;
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

    // The value of a signed and of an unsigned int as a long.
    public static long FromS(int x) => C(x < 0 ? 0xFFFFFFFFu : 0u, (uint)x);
    public static long FromU(uint x) => C(0, x);

    [MethodImpl(NI)] public static bool Same(long a, long b) => Lo(a) == Lo(b) && Hi(a) == Hi(b);

    public static string Hex(long x) => "0x" + Hi(x).ToString("x8") + Lo(x).ToString("x8");

    [MethodImpl(NI)]
    public static long AddC(long a, long b, uint cin)
    {
        uint* r = stackalloc uint[4];
        uint c = cin;
        for (int i = 0; i < 4; i++)
        {
            uint s = L(a, i) + L(b, i) + c;
            r[i] = s & 0xFFFF;
            c = s >> 16;
        }
        return Make(r[0], r[1], r[2], r[3]);
    }

    public static long Add(long a, long b) => AddC(a, b, 0);
    public static long Not(long a) => Make(L(a, 0) ^ 0xFFFF, L(a, 1) ^ 0xFFFF, L(a, 2) ^ 0xFFFF, L(a, 3) ^ 0xFFFF);
    public static long Neg(long a) => AddC(C(0, 0), Not(a), 1);

    // The low 64 bits of the product.
    [MethodImpl(NI)]
    public static long Mul(long a, long b)
    {
        uint* col = stackalloc uint[9];
        for (int i = 0; i < 9; i++) col[i] = 0;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                uint m = L(a, i) * L(b, j);
                col[i + j] += m & 0xFFFF;
                col[i + j + 1] += m >> 16;
            }
        }
        uint* p = stackalloc uint[4];
        uint c = 0;
        for (int k = 0; k < 4; k++)
        {
            uint s = col[k] + c;
            p[k] = s & 0xFFFF;
            c = s >> 16;
        }
        return Make(p[0], p[1], p[2], p[3]);
    }
}

unsafe class Program
{
    const MethodImplOptions NI = MethodImplOptions.NoInlining;

    static int fails;
    static int checks;
    static int s_calls;

    static void Fail(string name, long a, long b, string exp, string got)
    {
        fails++;
        if (fails <= 60)
            Console.WriteLine("FAIL " + name + " a=" + R.Hex(a) + " b=" + R.Hex(b) + " expected " + exp + " got " + got);
    }

    static void Check(string name, long a, long b, long got, long exp)
    {
        checks++;
        if (!R.Same(got, exp)) Fail(name, a, b, R.Hex(exp), R.Hex(got));
    }

    static void CheckI(string name, long a, long b, int got, int exp)
    {
        checks++;
        if (got != exp) Fail(name, a, b, exp.ToString(), got.ToString());
    }

    static void Group(string name)
    {
        Console.WriteLine("ok   " + name + " " + checks.ToString());
    }

    static readonly int[] I =
    {
        0, 1, -1, 2, -2, 0x7F, 0x80, 0x81, 0xFF, 0x100, -0x80, -0x81, -0x100,
        0x7FFF, 0x8000, 0x8001, 0xFFFF, 0x10000, -0x8000, -0x8001, -0x10000,
        0x7FFFFFFF, -0x7FFFFFFF, unchecked((int)0x80000000), 0x40000000, -0x40000000,
        0x12345678, unchecked((int)0x87654321), 0x0000B504, 0x0000B505, 46341, -46341, 65536, 65537,
    };

    static readonly long[] V =
    {
        0L, 1L, -1L, 0x7FL, 0x80L, 0xFFL, 0x100L, -0x80L, -0x81L,
        0x7FFFL, 0x8000L, 0xFFFFL, 0x10000L, -0x8000L, -0x8001L,
        0x7FFFFFFFL, 0x80000000L, 0xFFFFFFFFL, 0x100000000L, 0x100000001L, 0x1FFFFFFFFL,
        -0x80000000L, -0x80000001L, -0x100000000L, -0x100000001L, -0xFFFFFFFFL,
        0x7FFFFFFFFFFFFFFFL, unchecked((long)0x8000000000000000UL), unchecked((long)0x8000000000000001UL),
        0x0123456789ABCDEFL, unchecked((long)0xFEDCBA9876543210UL),
        unchecked((long)0xFFFFFFFF7FFFFFFFUL), 0x0000000180000000L, 0x7FFFFFFF00000000L,
        0x00000001_00000080L, unchecked((long)0xFFFFFFFF_FFFF8000UL), 0x00000001_FFFFFF80L,
    };

    // ---- widening ----

    [MethodImpl(NI)] static long IntToLong(int x) => x;
    [MethodImpl(NI)] static ulong IntToULong(int x) => (ulong)x;
    [MethodImpl(NI)] static long UIntToLong(uint x) => x;
    [MethodImpl(NI)] static ulong UIntToULong(uint x) => x;
    [MethodImpl(NI)] static long SByteToLong(sbyte x) => x;
    [MethodImpl(NI)] static long ByteToLong(byte x) => x;
    [MethodImpl(NI)] static long ShortToLong(short x) => x;
    [MethodImpl(NI)] static long UShortToLong(ushort x) => x;
    [MethodImpl(NI)] static long CharToLong(char x) => x;
    [MethodImpl(NI)] static long BoolToLong(bool x) => x ? 1L : 0L;
    [MethodImpl(NI)] static ulong SByteToULong(sbyte x) => (ulong)x;
    [MethodImpl(NI)] static ulong ShortToULong(short x) => (ulong)x;
    [MethodImpl(NI)] static long NIntToLong(nint x) => x;
    [MethodImpl(NI)] static ulong NUIntToULong(nuint x) => x;
    [MethodImpl(NI)] static long NUIntToLong(nuint x) => (long)x;
    [MethodImpl(NI)] static ulong NIntToULong(nint x) => (ulong)x;
    [MethodImpl(NI)] static long PtrToLong(void* p) => (long)p;
    [MethodImpl(NI)] static ulong PtrToULong(void* p) => (ulong)p;

    // Widening of a value that is narrowed in the same expression, and of values in memory.
    [MethodImpl(NI)] static long IntAsSByteToLong(int x) => (sbyte)x;
    [MethodImpl(NI)] static long IntAsByteToLong(int x) => (byte)x;
    [MethodImpl(NI)] static long IntAsShortToLong(int x) => (short)x;
    [MethodImpl(NI)] static long IntAsUShortToLong(int x) => (ushort)x;
    [MethodImpl(NI)] static long LongAsIntToLong(long x) => (int)x;
    [MethodImpl(NI)] static long LongAsUIntToLong(long x) => (uint)x;
    [MethodImpl(NI)] static long LongAsSByteToLong(long x) => (sbyte)x;
    [MethodImpl(NI)] static long LongAsByteToLong(long x) => (byte)x;
    [MethodImpl(NI)] static long LongAsShortToLong(long x) => (short)x;
    [MethodImpl(NI)] static long LongAsUShortToLong(long x) => (ushort)x;
    [MethodImpl(NI)] static long LoadInt(int* p) => *p;
    [MethodImpl(NI)] static long LoadUInt(uint* p) => *p;
    [MethodImpl(NI)] static long LoadSByte(sbyte* p) => *p;
    [MethodImpl(NI)] static long LoadByte(byte* p) => *p;
    [MethodImpl(NI)] static long LoadShort(short* p) => *p;
    [MethodImpl(NI)] static long LoadUShort(ushort* p) => *p;
    [MethodImpl(NI)] static void StoreWide(int x, long* p) { p[0] = x; p[1] = (uint)x; }
    [MethodImpl(NI)] static long WidenAdd(int x, int y) => (long)x + y;
    [MethodImpl(NI)] static long WidenAddU(uint x, uint y) => (long)x + y;
    [MethodImpl(NI)] static long WidenSub(int x, uint y) => x - (long)y;
    [MethodImpl(NI)] static long SumInts(int[] a) { long s = 0; for (int i = 0; i < a.Length; i++) s += a[i]; return s; }
    [MethodImpl(NI)] static ulong SumUInts(int[] a) { ulong s = 0; for (int i = 0; i < a.Length; i++) s += (uint)a[i]; return s; }

    // ---- narrowing ----

    [MethodImpl(NI)] static int LongToInt(long x) => (int)x;
    [MethodImpl(NI)] static uint LongToUInt(long x) => (uint)x;
    [MethodImpl(NI)] static short LongToShort(long x) => (short)x;
    [MethodImpl(NI)] static ushort LongToUShort(long x) => (ushort)x;
    [MethodImpl(NI)] static sbyte LongToSByte(long x) => (sbyte)x;
    [MethodImpl(NI)] static byte LongToByte(long x) => (byte)x;
    [MethodImpl(NI)] static char LongToChar(long x) => (char)x;
    [MethodImpl(NI)] static bool LongToBool(long x) => x != 0;
    [MethodImpl(NI)] static nint LongToNInt(long x) => (nint)x;
    [MethodImpl(NI)] static nuint ULongToNUInt(ulong x) => (nuint)x;
    [MethodImpl(NI)] static int LongSumToInt(long x, long y) => (int)(x + y);
    [MethodImpl(NI)] static int LongProductToInt(long x, long y) => (int)(x * y);
    [MethodImpl(NI)] static int IndexWithLong(int[] a, long i) => a[i];
    [MethodImpl(NI)] static void StoreNarrow(long x, int* p) { p[0] = (int)x; p[1] = (int)(x >> 32); }

    // A narrowing whose source has a side effect in each half of its evaluation.
    [MethodImpl(NI)] static long Counted(long x) { s_calls++; return x; }
    [MethodImpl(NI)] static int NarrowCall(long x) => (int)Counted(x);
    [MethodImpl(NI)] static void DiscardCall(long x) { _ = (int)Counted(x); }

    // ---- checked ----

    [MethodImpl(NI)] static int LongToIntOvf(long x) => checked((int)x);
    [MethodImpl(NI)] static uint LongToUIntOvf(long x) => checked((uint)x);
    [MethodImpl(NI)] static int ULongToIntOvf(ulong x) => checked((int)x);
    [MethodImpl(NI)] static uint ULongToUIntOvf(ulong x) => checked((uint)x);
    [MethodImpl(NI)] static ulong LongToULongOvf(long x) => checked((ulong)x);
    [MethodImpl(NI)] static long ULongToLongOvf(ulong x) => checked((long)x);
    [MethodImpl(NI)] static short LongToShortOvf(long x) => checked((short)x);
    [MethodImpl(NI)] static ushort LongToUShortOvf(long x) => checked((ushort)x);
    [MethodImpl(NI)] static sbyte LongToSByteOvf(long x) => checked((sbyte)x);
    [MethodImpl(NI)] static byte LongToByteOvf(long x) => checked((byte)x);
    [MethodImpl(NI)] static short ULongToShortOvf(ulong x) => checked((short)x);
    [MethodImpl(NI)] static ushort ULongToUShortOvf(ulong x) => checked((ushort)x);
    [MethodImpl(NI)] static sbyte ULongToSByteOvf(ulong x) => checked((sbyte)x);
    [MethodImpl(NI)] static byte ULongToByteOvf(ulong x) => checked((byte)x);
    [MethodImpl(NI)] static nint LongToNIntOvf(long x) => checked((nint)x);
    [MethodImpl(NI)] static nuint LongToNUIntOvf(long x) => checked((nuint)x);
    [MethodImpl(NI)] static ulong IntToULongOvf(int x) => checked((ulong)x);
    [MethodImpl(NI)] static uint IntToUIntOvf(int x) => checked((uint)x);
    [MethodImpl(NI)] static int UIntToIntOvf(uint x) => checked((int)x);

    // ---- 32x32->64 ----

    [MethodImpl(NI)] static long MulWide(int a, int b) => (long)a * b;
    [MethodImpl(NI)] static ulong MulWideU(uint a, uint b) => (ulong)a * b;
    [MethodImpl(NI)] static long MulWideMixed(int a, uint b) => (long)a * b;
    [MethodImpl(NI)] static long MulWideMixed2(uint a, int b) => (long)a * b;
    [MethodImpl(NI)] static long BigMul(int a, int b) => Math.BigMul(a, b);
    [MethodImpl(NI)] static ulong BigMulU(uint a, uint b) => Math.BigMul(a, b);
    [MethodImpl(NI)] static int MulHigh(int a, int b) => (int)(((long)a * b) >> 32);
    [MethodImpl(NI)] static uint MulHighU(uint a, uint b) => (uint)(((ulong)a * b) >> 32);
    [MethodImpl(NI)] static long MulWideConst(int a) => (long)a * 1000000007;
    [MethodImpl(NI)] static ulong MulWideConstU(uint a) => (ulong)a * 0xFFFFFFFBu;
    [MethodImpl(NI)] static long MulWideSquare(int a) => (long)a * a;
    [MethodImpl(NI)] static long MulLongByInt(long a, int b) => a * b;
    [MethodImpl(NI)] static long MulLongByUInt(long a, uint b) => a * b;
    [MethodImpl(NI)] static long MulWideAdd(int a, int b, long c) => (long)a * b + c;
    [MethodImpl(NI)] static ulong MulWideAddU(uint a, uint b, uint c, uint d) => (ulong)a * b + c + d;
    [MethodImpl(NI)] static long MulWideTwice(int a, int b) => (long)a * b + (long)b * a;
    [MethodImpl(NI)] static long MulWideSmall(short a, sbyte b) => (long)a * b;
    [MethodImpl(NI)] static long MulWideFromMem(int* p) => (long)p[0] * p[1];
    [MethodImpl(NI)]
    static long Dot(int[] a, int[] b)
    {
        long s = 0;
        for (int i = 0; i < a.Length; i++) s += (long)a[i] * b[i];
        return s;
    }

    delegate long Conv(long x);

    // 0 = no exception, 1 = OverflowException, 3 = any other.
    static int Run(Conv op, long x, out long r)
    {
        r = 0;
        try { r = op(x); return 0; }
        catch (OverflowException) { return 1; }
        catch (Exception) { return 3; }
    }

    static void CheckOvf(string name, long a, Conv op, bool expOvf, long exp)
    {
        checks++;
        int kind = Run(op, a, out long got);
        if (kind != (expOvf ? 1 : 0))
        {
            Fail(name, a, 0, expOvf ? "OverflowException" : "no exception", "kind " + kind.ToString());
            return;
        }
        if (!expOvf && !R.Same(got, exp)) Fail(name, a, 0, R.Hex(exp), R.Hex(got));
    }

    static void TestWiden()
    {
        long* two = stackalloc long[2];
        foreach (int x in I)
        {
            long s = R.FromS(x), u = R.FromU((uint)x);
            Check("int -> long", x, 0, IntToLong(x), s);
            Check("int -> ulong", x, 0, (long)IntToULong(x), s);
            Check("uint -> long", x, 0, UIntToLong((uint)x), u);
            Check("uint -> ulong", x, 0, (long)UIntToULong((uint)x), u);
            Check("nint -> long", x, 0, NIntToLong(x), s);
            Check("nuint -> ulong", x, 0, (long)NUIntToULong((nuint)(uint)x), u);
            Check("nuint -> long", x, 0, NUIntToLong((nuint)(uint)x), u);
            if (sizeof(nint) == 4)
            {
                Check("nint -> ulong", x, 0, (long)NIntToULong(x), s);
                Check("pointer -> long", x, 0, PtrToLong((void*)(uint)x), u);
                Check("pointer -> ulong", x, 0, (long)PtrToULong((void*)(uint)x), u);
            }

            long s8 = R.FromS((x << 24) >> 24), u8 = R.FromU((uint)x & 0xFF);
            long s16 = R.FromS((x << 16) >> 16), u16 = R.FromU((uint)x & 0xFFFF);
            Check("sbyte -> long", x, 0, SByteToLong((sbyte)x), s8);
            Check("byte -> long", x, 0, ByteToLong((byte)x), u8);
            Check("short -> long", x, 0, ShortToLong((short)x), s16);
            Check("ushort -> long", x, 0, UShortToLong((ushort)x), u16);
            Check("char -> long", x, 0, CharToLong((char)x), u16);
            Check("sbyte -> ulong", x, 0, (long)SByteToULong((sbyte)x), s8);
            Check("short -> ulong", x, 0, (long)ShortToULong((short)x), s16);
            Check("(sbyte)int -> long", x, 0, IntAsSByteToLong(x), s8);
            Check("(byte)int -> long", x, 0, IntAsByteToLong(x), u8);
            Check("(short)int -> long", x, 0, IntAsShortToLong(x), s16);
            Check("(ushort)int -> long", x, 0, IntAsUShortToLong(x), u16);
            Check("bool -> long", x, 0, BoolToLong(x != 0), R.FromU(x != 0 ? 1u : 0u));

            int cell = x;
            Check("load int", x, 0, LoadInt(&cell), s);
            Check("load uint", x, 0, LoadUInt((uint*)&cell), u);
            Check("load sbyte", x, 0, LoadSByte((sbyte*)&cell), s8);
            Check("load byte", x, 0, LoadByte((byte*)&cell), u8);
            Check("load short", x, 0, LoadShort((short*)&cell), s16);
            Check("load ushort", x, 0, LoadUShort((ushort*)&cell), u16);
            StoreWide(x, two);
            Check("store signed", x, 0, two[0], s);
            Check("store unsigned", x, 0, two[1], u);

            foreach (int y in I)
            {
                Check("widen add", x, y, WidenAdd(x, y), R.Add(s, R.FromS(y)));
                Check("widen add u", x, y, WidenAddU((uint)x, (uint)y), R.Add(u, R.FromU((uint)y)));
                Check("widen sub", x, y, WidenSub(x, (uint)y), R.Add(s, R.Neg(R.FromU((uint)y))));
            }
        }

        long sum = R.C(0, 0), usum = R.C(0, 0);
        foreach (int x in I) { sum = R.Add(sum, R.FromS(x)); usum = R.Add(usum, R.FromU((uint)x)); }
        Check("sum ints", 0, 0, SumInts(I), sum);
        Check("sum uints", 0, 0, (long)SumUInts(I), usum);
        Group("widen");
    }

    static void TestNarrow()
    {
        int* two = stackalloc int[2];
        int[] table = new int[40];
        for (int i = 0; i < table.Length; i++) table[i] = 1000 + i * 7;

        foreach (long a in V)
        {
            int lo = (int)R.Lo(a);
            CheckI("long -> int", a, 0, LongToInt(a), lo);
            CheckI("long -> uint", a, 0, (int)LongToUInt(a), lo);
            CheckI("long -> short", a, 0, LongToShort(a), (lo << 16) >> 16);
            CheckI("long -> ushort", a, 0, LongToUShort(a), lo & 0xFFFF);
            CheckI("long -> sbyte", a, 0, LongToSByte(a), (lo << 24) >> 24);
            CheckI("long -> byte", a, 0, LongToByte(a), lo & 0xFF);
            CheckI("long -> char", a, 0, LongToChar(a), lo & 0xFFFF);
            CheckI("long -> bool", a, 0, LongToBool(a) ? 1 : 0, (R.Lo(a) | R.Hi(a)) != 0 ? 1 : 0);
            if (sizeof(nint) == 4)
            {
                CheckI("long -> nint", a, 0, (int)LongToNInt(a), lo);
                CheckI("ulong -> nuint", a, 0, (int)(uint)ULongToNUInt((ulong)a), lo);
            }

            Check("(int)long -> long", a, 0, LongAsIntToLong(a), R.FromS(lo));
            Check("(uint)long -> long", a, 0, LongAsUIntToLong(a), R.FromU((uint)lo));
            Check("(sbyte)long -> long", a, 0, LongAsSByteToLong(a), R.FromS((lo << 24) >> 24));
            Check("(byte)long -> long", a, 0, LongAsByteToLong(a), R.FromU((uint)lo & 0xFF));
            Check("(short)long -> long", a, 0, LongAsShortToLong(a), R.FromS((lo << 16) >> 16));
            Check("(ushort)long -> long", a, 0, LongAsUShortToLong(a), R.FromU((uint)lo & 0xFFFF));

            StoreNarrow(a, two);
            CheckI("store low", a, 0, two[0], lo);
            CheckI("store high", a, 0, two[1], (int)R.Hi(a));

            s_calls = 0;
            CheckI("narrow a call", a, 0, NarrowCall(a), lo);
            DiscardCall(a);
            CheckI("narrow a call: calls", a, 0, s_calls, 2);

            foreach (long b in V)
            {
                CheckI("(int)(a + b)", a, b, LongSumToInt(a, b), (int)(R.Lo(a) + R.Lo(b)));
                CheckI("(int)(a * b)", a, b, LongProductToInt(a, b), (int)(R.Lo(a) * R.Lo(b)));
            }
        }

        for (int i = 0; i < table.Length; i++)
            CheckI("long index", i, 0, IndexWithLong(table, R.FromS(i)), 1000 + i * 7);
        Group("narrow");
    }

    static void TestChecked()
    {
        foreach (long a in V)
        {
            uint hi = R.Hi(a), lo = R.Lo(a);
            bool fitsInt = hi == ((int)lo < 0 ? 0xFFFFFFFFu : 0u);
            bool fitsUInt = hi == 0;
            bool fitsIntU = hi == 0 && (int)lo >= 0;
            bool neg = (int)hi < 0;
            int ilo = (int)lo;

            CheckOvf("long -> int ovf", a, x => LongToIntOvf(x), !fitsInt, R.FromS(ilo));
            CheckOvf("long -> uint ovf", a, x => LongToUIntOvf(x), !fitsUInt, R.FromU(lo));
            CheckOvf("ulong -> int ovf", a, x => ULongToIntOvf((ulong)x), !fitsIntU, R.FromS(ilo));
            CheckOvf("ulong -> uint ovf", a, x => ULongToUIntOvf((ulong)x), !fitsUInt, R.FromU(lo));
            CheckOvf("long -> ulong ovf", a, x => (long)LongToULongOvf(x), neg, a);
            CheckOvf("ulong -> long ovf", a, x => ULongToLongOvf((ulong)x), neg, a);

            CheckOvf("long -> short ovf", a, x => LongToShortOvf(x), !(fitsInt && ilo >= -0x8000 && ilo <= 0x7FFF), R.FromS(ilo));
            CheckOvf("long -> ushort ovf", a, x => LongToUShortOvf(x), !(fitsUInt && lo <= 0xFFFF), R.FromU(lo));
            CheckOvf("long -> sbyte ovf", a, x => LongToSByteOvf(x), !(fitsInt && ilo >= -0x80 && ilo <= 0x7F), R.FromS(ilo));
            CheckOvf("long -> byte ovf", a, x => LongToByteOvf(x), !(fitsUInt && lo <= 0xFF), R.FromU(lo));
            CheckOvf("ulong -> short ovf", a, x => ULongToShortOvf((ulong)x), !(fitsUInt && lo <= 0x7FFF), R.FromS(ilo));
            CheckOvf("ulong -> ushort ovf", a, x => ULongToUShortOvf((ulong)x), !(fitsUInt && lo <= 0xFFFF), R.FromU(lo));
            CheckOvf("ulong -> sbyte ovf", a, x => ULongToSByteOvf((ulong)x), !(fitsUInt && lo <= 0x7F), R.FromS(ilo));
            CheckOvf("ulong -> byte ovf", a, x => ULongToByteOvf((ulong)x), !(fitsUInt && lo <= 0xFF), R.FromU(lo));
            if (sizeof(nint) == 4)
            {
                CheckOvf("long -> nint ovf", a, x => LongToNIntOvf(x), !fitsInt, R.FromS(ilo));
                CheckOvf("long -> nuint ovf", a, x => (long)(ulong)LongToNUIntOvf(x), !fitsUInt, R.FromU(lo));
            }
        }

        foreach (int i in I)
        {
            long s = R.FromS(i);
            CheckOvf("int -> ulong ovf", s, x => (long)IntToULongOvf((int)R.Lo(x)), i < 0, s);
            CheckOvf("int -> uint ovf", s, x => IntToUIntOvf((int)R.Lo(x)), i < 0, R.FromU((uint)i));
            CheckOvf("uint -> int ovf", s, x => UIntToIntOvf(R.Lo(x)), i < 0, s);
        }
        Group("checked");
    }

    static void TestMulWide()
    {
        int* pair = stackalloc int[2];
        long dot = R.C(0, 0);
        int[] rev = new int[I.Length];
        for (int i = 0; i < I.Length; i++) rev[i] = I[I.Length - 1 - i];

        for (int i = 0; i < I.Length; i++)
        {
            int a = I[i];
            long sa = R.FromS(a), ua = R.FromU((uint)a);
            foreach (int b in I)
            {
                long sb = R.FromS(b), ub = R.FromU((uint)b);
                long ss = R.Mul(sa, sb), uu = R.Mul(ua, ub);
                Check("mul wide", a, b, MulWide(a, b), ss);
                Check("mul wide u", a, b, (long)MulWideU((uint)a, (uint)b), uu);
                Check("mul wide int*uint", a, b, MulWideMixed(a, (uint)b), R.Mul(sa, ub));
                Check("mul wide uint*int", a, b, MulWideMixed2((uint)a, b), R.Mul(ua, sb));
                Check("BigMul", a, b, BigMul(a, b), ss);
                Check("BigMul u", a, b, (long)BigMulU((uint)a, (uint)b), uu);
                CheckI("mul high", a, b, MulHigh(a, b), (int)R.Hi(ss));
                CheckI("mul high u", a, b, (int)MulHighU((uint)a, (uint)b), (int)R.Hi(uu));
                Check("mul wide twice", a, b, MulWideTwice(a, b), R.Add(ss, ss));
                Check("mul wide small", a, b, MulWideSmall((short)a, (sbyte)b),
                      R.Mul(R.FromS((a << 16) >> 16), R.FromS((b << 24) >> 24)));
                pair[0] = a; pair[1] = b;
                Check("mul wide from memory", a, b, MulWideFromMem(pair), ss);
                Check("mul wide add u", a, b, (long)MulWideAddU((uint)a, (uint)b, (uint)a, (uint)b), R.Add(R.Add(uu, ua), ub));
                foreach (long c in V)
                {
                    Check("mul wide add", a, b, MulWideAdd(a, b, c), R.Add(ss, c));
                }
            }
            Check("mul wide const", a, 0, MulWideConst(a), R.Mul(sa, R.FromS(1000000007)));
            Check("mul wide const u", a, 0, (long)MulWideConstU((uint)a), R.Mul(ua, R.FromU(0xFFFFFFFBu)));
            Check("mul wide square", a, 0, MulWideSquare(a), R.Mul(sa, sa));
            foreach (long c in V)
            {
                Check("long * int", c, a, MulLongByInt(c, a), R.Mul(c, sa));
                Check("long * uint", c, a, MulLongByUInt(c, (uint)a), R.Mul(c, ua));
            }
            dot = R.Add(dot, R.Mul(sa, R.FromS(rev[i])));
        }
        Check("dot product", 0, 0, Dot(I, rev), dot);
        Group("32x32->64");
    }

    static int Main()
    {
        TestWiden();
        TestNarrow();
        TestChecked();
        TestMulWide();

        if (fails == 0)
        {
            Console.WriteLine("ALL PASS");
            return 0;
        }
        Console.WriteLine("FAIL " + fails.ToString());
        return 1;
    }
}
