// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// 32-bit loads that leave the upper half of the register zero, loads that
// are narrowed, and zero extensions replaced by sign extensions.
//
// An int is kept sign-extended in its register, so an int load is an lw.
// Where the single user of a load reads the same result from either
// extension, the compiler loads with lwu: a store of 4 bytes or fewer, a
// 32-bit add, subtract, multiply, negate or shift, a shift count, a cast to
// a small type, an AND with a value that is never negative, an equality or
// unsigned compare against such a value, the index or the length of a
// bounds check. A cast to a small type or a mask with 0xFF or 0xFFFF of a
// load becomes a narrower load. And a zero extension of a value that is
// known not to be negative becomes a sign extension, which is dropped when
// the register is already extended. What can go wrong: a user on that list
// that does read the upper half (a 64-bit instruction chosen for a 32-bit
// node, a compare against a value that can be negative after all), a load
// that has a second reader, a reload after a spill, a narrowed load at the
// wrong address or of the wrong signedness, and a value taken for
// non-negative that is negative on some path (after a change behind the
// check, in a loop, in a handler).
//
// The loads here read values with bit 31 set from arrays, object and
// static fields, struct fields, pointers and locals whose address was
// taken. Every case has its expected value computed by code compiled
// without optimization from the same values kept as 64-bit numbers.
//
// A failed check prints one line; the run ends with ALL PASS or a FAIL
// count, and the guest returns 1 when anything failed.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class Box
{
    public long Pad;
    public int I;
    public uint U;
    public int J;
    public short S;
    public byte B;
}

struct Pair { public int X; public int Y; }

unsafe class Program
{
    const MethodImplOptions NI = MethodImplOptions.NoInlining;
    const MethodImplOptions NoOpt = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;

    static int fails;
    static int checks;
    static int shown;

    static readonly long[] Source =
    {
        0, 1, -1, 0x7FFFFFFF, -0x80000000L, 0x80, 0xFF, 0x7FFF, 0x8000, 0xFFFF, -0x8000, -0x81,
        0x12345678, -0x12345678, 0x40000000, -0x40000001, 5, 100, -2, 0x00FF00FF, -0x00FF0100, 31, 32, 33,
    };

    const int N = 24;

    static int[] Arr;
    static uint[] ArrU;
    static Pair[] Pairs;
    static Box Obj;
    static int Static;
    static long[] Tab;
    static int[] Out;
    static short[] OutS;
    static byte[] OutB;
    static int Cell;

    [MethodImpl(NoOpt)]
    static void Setup()
    {
        Arr = new int[N]; ArrU = new uint[N]; Pairs = new Pair[N];
        for (int i = 0; i < N; i++)
        {
            Arr[i] = (int)Source[i];
            ArrU[i] = (uint)Source[i];
            Pairs[i].X = (int)Source[i];
            Pairs[i].Y = (int)~Source[i];
        }
        Obj = new Box();
        Tab = new long[16];
        for (int i = 0; i < 16; i++) Tab[i] = (i + 1) * 0x0101010101010101L;
        Out = new int[4]; OutS = new short[4]; OutB = new byte[4];
    }

    [MethodImpl(NI)] static void Opaque() { Cell ^= 0; }
    [MethodImpl(NI)] static int Zero() => Cell & 0;
    [MethodImpl(NI)] static void Touch(int* p) { Cell ^= *p & 0; }

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
    static long S8(long v)
    {
        v &= 0xFF;
        if (v >= 0x80) v -= 0x100;
        return v;
    }

    [MethodImpl(NoOpt)] static long B(bool f) => f ? 1 : 0;

    // ---- the load feeds one 32-bit user; the result is widened ----
    // i selects the value loaded, b is a second int, m a value that is never negative.

    [MethodImpl(NI)] static long AddW(int i, int b) => (long)(Arr[i] + b);
    [MethodImpl(NI)] static long SubW(int i, int b) => (long)(b - Arr[i]);
    [MethodImpl(NI)] static long MulW(int i, int b) => (long)(Arr[i] * b);
    [MethodImpl(NI)] static long NegW(int i, int b) => (long)(-Arr[i]);
    [MethodImpl(NI)] static long ShlW(int i, int b) => (long)(Arr[i] << b);
    [MethodImpl(NI)] static long SarW(int i, int b) => (long)(Arr[i] >> b);
    [MethodImpl(NI)] static long ShrW(int i, int b) => (long)(int)((uint)Arr[i] >> b);
    [MethodImpl(NI)] static long SarC(int i, int b) => (long)(Arr[i] >> 3);
    [MethodImpl(NI)] static long Sar0(int i, int b) => (long)(Arr[i] >> (b & 0));
    [MethodImpl(NI)] static long ShrZ(int i, int b) => (long)((uint)Arr[i] >> 1);
    [MethodImpl(NI)] static long Count(int i, long k) => (k << Arr[i]) ^ (k >> Arr[i]) ^ (long)((ulong)k >> Arr[i]);
    [MethodImpl(NI)] static long CountW(int i, int b) => (long)(b << Arr[i]) ^ (long)(b >> Arr[i]);
    [MethodImpl(NI)] static long AddTwo(int i, int j) => (long)(Arr[i] + Arr[j]);
    [MethodImpl(NI)] static long AddWiden(int i, long k) => (long)Arr[i] + k;
    [MethodImpl(NI)] static long AddWidenU(int i, long k) => (long)ArrU[i] + k;
    [MethodImpl(NI)] static long MulWiden(int i, int j) => (long)Arr[i] * Arr[j];
    [MethodImpl(NI)] static long AndMax(int i, int b) => (long)(Arr[i] & 0x7FFFFFFF);
    [MethodImpl(NI)] static long AndSmall(int i, int b) => (long)(Arr[i] & 0x7F0);
    [MethodImpl(NI)] static long AndByte(int i, int b) => (long)(Arr[i] & (byte)b);
    [MethodImpl(NI)] static long AndChar(int i, int b) => (long)(Arr[i] & (ushort)b) + (long)(uint)(Arr[i] & (ushort)b);
    [MethodImpl(NI)] static long AndCompare(int i, int b) => (long)(Arr[i] & (b < 7 ? 1 : 0));
    [MethodImpl(NI)] static long AndShr(int i, int b) => (long)(Arr[i] & (int)((uint)b >> 1));
    [MethodImpl(NI)] static long AndInt(int i, int b) => (long)(Arr[i] & b);
    [MethodImpl(NI)] static long AndNeg(int i, int b) => (long)(Arr[i] & unchecked((int)0x80000001));
    [MethodImpl(NI)] static long OrInt(int i, int b) => (long)(Arr[i] | (b & 0xFF));
    [MethodImpl(NI)] static long XorInt(int i, int b) => (long)(Arr[i] ^ (b & 0xFF));
    [MethodImpl(NI)] static long AndLength(int i, int[] a) => (long)(Arr[i] & a.Length);
    [MethodImpl(NI)] static long Div(int i, int b) => (long)(Arr[i] / ((b & 0xFF) + 2));
    [MethodImpl(NI)] static long DivBy(int i, int b) => (long)((b >> 1) / (Arr[i] | 0x40000000));
    [MethodImpl(NI)] static long Mod(int i, int b) => (long)(Arr[i] % ((b & 0xFF) + 2));
    [MethodImpl(NI)] static long UDiv(int i, int b) => (long)((uint)Arr[i] / (uint)((b & 0xFF) + 2));

    // ---- compares against a value that is never negative ----

    [MethodImpl(NI)] static bool EqConst(int i) => Arr[i] == 100;
    [MethodImpl(NI)] static bool NeConst(int i) => Arr[i] != 0x7FFFFFFF;
    [MethodImpl(NI)] static bool EqZero(int i) => Arr[i] == 0;
    [MethodImpl(NI)] static bool EqNegConst(int i) => Arr[i] == -1;
    [MethodImpl(NI)] static bool EqMin(int i) => Arr[i] == int.MinValue;
    [MethodImpl(NI)] static bool UltConst(int i) => (uint)Arr[i] < 100u;
    [MethodImpl(NI)] static bool UgtConst(int i) => (uint)Arr[i] > 0x7FFFFFFFu;
    [MethodImpl(NI)] static bool UgeConst(int i) => (uint)Arr[i] >= 0x7FFFFFFFu;
    [MethodImpl(NI)] static bool UltBig(int i) => (uint)Arr[i] < 0x80000000u;
    [MethodImpl(NI)] static bool UltHuge(int i) => (uint)Arr[i] < 0xFFFFFFFFu;
    [MethodImpl(NI)] static bool LtConst(int i) => Arr[i] < 100;
    [MethodImpl(NI)] static bool GtConst(int i) => Arr[i] > 100;
    [MethodImpl(NI)] static bool LtZero(int i) => Arr[i] < 0;
    [MethodImpl(NI)] static bool GeZero(int i) => Arr[i] >= 0;
    [MethodImpl(NI)] static bool EqByte(int i, int b) => Arr[i] == (byte)b;
    [MethodImpl(NI)] static bool EqChar(int i, int b) => (ushort)b == Arr[i];
    [MethodImpl(NI)] static bool UltByte(int i, int b) => (uint)Arr[i] < (uint)(byte)b;
    [MethodImpl(NI)] static bool UgtMasked(int i, int b) => (uint)Arr[i] > (uint)(b & 0x7FFFFFFF);
    [MethodImpl(NI)] static bool EqMasked(int i, int b) => Arr[i] == (b & 0x7FFFFFFF);
    [MethodImpl(NI)] static bool EqInt(int i, int b) => Arr[i] == b;
    [MethodImpl(NI)] static bool UltInt(int i, int b) => (uint)Arr[i] < (uint)b;
    [MethodImpl(NI)] static bool EqLength(int i, int[] a) => Arr[i] == a.Length;
    [MethodImpl(NI)] static bool UltLength(int i, int[] a) => (uint)Arr[i] < (uint)a.Length;
    [MethodImpl(NI)] static bool EqTwo(int i, int j) => Arr[i] == Arr[j];
    [MethodImpl(NI)] static bool UltTwo(int i, int j) => (uint)Arr[i] < (uint)Arr[j];
    [MethodImpl(NI)] static bool EqCompare(int i, int b) => Arr[i] == (b < 7 ? 1 : 0);

    [MethodImpl(NI)]
    static long Branches(int i, int b)
    {
        long r = 0;
        if (Arr[i] == 100) r |= 1;
        if (Arr[i] != 0x7FFFFFFF) r |= 2;
        if ((uint)Arr[i] < 100u) r |= 4;
        if ((uint)Arr[i] > 0x7FFFFFFFu) r |= 8;
        if (Arr[i] == (byte)b) r |= 16;
        if ((uint)Arr[i] <= (uint)(b & 0x7FFFFFFF)) r |= 32;
        if (Arr[i] == 0) r |= 64;
        if (Arr[i] < 100) r |= 128;
        if (Arr[i] == -1) r |= 256;
        if (Arr[i] < 0) r |= 512;
        if ((uint)Arr[i] >= (uint)Tab.Length) r |= 1024;
        if (Arr[i] == Tab.Length) r |= 2048;
        if ((uint)(byte)b >= (uint)Arr[i]) r |= 4096;
        return r;
    }

    [MethodImpl(NoOpt)]
    static long RBranches(long v, long b)
    {
        long r = 0;
        if (v == 100) r |= 1;
        if (v != 0x7FFFFFFF) r |= 2;
        if (Z32(v) < 100) r |= 4;
        if (Z32(v) > 0x7FFFFFFFL) r |= 8;
        if (v == (b & 0xFF)) r |= 16;
        if (Z32(v) <= (b & 0x7FFFFFFF)) r |= 32;
        if (v == 0) r |= 64;
        if (v < 100) r |= 128;
        if (v == -1) r |= 256;
        if (v < 0) r |= 512;
        if (Z32(v) >= 16) r |= 1024;
        if (v == 16) r |= 2048;
        if ((b & 0xFF) >= Z32(v)) r |= 4096;
        return r;
    }

    // ---- bounds checks: the index is loaded, the length is loaded ----

    [MethodImpl(NI)]
    static long IndexLoaded(int i)
    {
        try { return Tab[Arr[i]]; }
        catch (IndexOutOfRangeException) { return -2; }
    }

    [MethodImpl(NI)]
    static long IndexSpan(int i)
    {
        Span<long> s = stackalloc long[8];
        for (int j = 0; j < 8; j++) s[j] = j * 3 + 1;
        try { return s[Arr[i]]; }
        catch (IndexOutOfRangeException) { return -2; }
    }

    [MethodImpl(NI)]
    static long IndexConstLength(int i)
    {
        ReadOnlySpan<byte> s = new byte[] { 11, 12, 13, 14, 15, 16, 17, 18 };
        try { return s[Arr[i]]; }
        catch (IndexOutOfRangeException) { return -2; }
    }

    [MethodImpl(NI)]
    static long IndexGuarded(int i)
    {
        long[] t = Tab;
        return (uint)Arr[i] < (uint)t.Length ? t[Arr[i]] : -2;
    }

    [MethodImpl(NI)]
    static long LengthLoaded(int[] a, int idx)
    {
        try { return a[idx]; }
        catch (IndexOutOfRangeException) { return -2; }
    }

    [MethodImpl(NI)]
    static long SpanLength(Span<int> s, int idx)
    {
        try { return s[idx]; }
        catch (IndexOutOfRangeException) { return -2; }
    }

    [MethodImpl(NI)] static long LengthUses(int[] a, int b) => (long)a.Length * 3 + (a.Length < b ? 5 : 7) + ((uint)b < (uint)a.Length ? 11 : 13) + (long)(uint)a.Length + (a.Length - b);

    // ---- stores of the low bytes ----

    [MethodImpl(NI)]
    static long Stores(int i)
    {
        Out[1] = Arr[i];
        OutS[1] = (short)Arr[i];
        OutB[1] = (byte)Arr[i];
        Obj.J = Arr[i];
        Obj.S = (short)Arr[i];
        Obj.B = (byte)Arr[i];
        Static = Arr[i];
        Opaque();
        return (long)Out[1] + OutS[1] * 3L + OutB[1] * 5L + (long)Obj.J * 7 + Obj.S * 11L + Obj.B * 13L + (long)Static * 17;
    }

    [MethodImpl(NoOpt)] static long RStores(long v) => v + S16(v) * 3 + (v & 0xFF) * 5 + v * 7 + S16(v) * 11 + (v & 0xFF) * 13 + v * 17;

    [MethodImpl(NI)]
    static long StoreLocalField(int i)
    {
        Pair p;
        p.X = Arr[i];
        p.Y = Arr[i] + 1;
        Touch(&p.X);
        return (long)p.X * 3 + p.Y;
    }

    // ---- narrowed loads ----

    [MethodImpl(NI)] static long Narrow(int i) => (long)(byte)Arr[i] + (long)(sbyte)Arr[i] * 3 + (long)(short)Arr[i] * 5 + (long)(ushort)Arr[i] * 7 + (long)(Arr[i] & 0xFF) * 11 + (long)(Arr[i] & 0xFFFF) * 13;
    [MethodImpl(NI)] static long NarrowU(int i) => (long)(byte)ArrU[i] + (long)(sbyte)ArrU[i] * 3 + (long)(short)ArrU[i] * 5 + (long)(ushort)ArrU[i] * 7 + (long)(ArrU[i] & 0xFF) * 11 + (long)(ArrU[i] & 0xFFFF) * 13;
    [MethodImpl(NoOpt)] static long RNarrow(long v) => (v & 0xFF) + S8(v) * 3 + S16(v) * 5 + (v & 0xFFFF) * 7 + (v & 0xFF) * 11 + (v & 0xFFFF) * 13;

    [MethodImpl(NI)] static long NarrowField(Box o) => (long)(byte)o.I + (long)(sbyte)o.I * 3 + (long)(short)o.I * 5 + (long)(ushort)o.I * 7 + (long)(o.I & 0xFF) * 11 + (long)(o.I & 0xFFFF) * 13;
    [MethodImpl(NI)] static long NarrowPair(int i) => (long)(byte)Pairs[i].Y + (long)(short)Pairs[i].Y * 5 + (long)(Pairs[i].Y & 0xFFFF) * 13 + (long)(sbyte)Pairs[i].X * 3 + (long)(ushort)Pairs[i].X * 7 + (long)(Pairs[i].X & 0xFF) * 11;
    [MethodImpl(NoOpt)] static long RNarrowPair(long x, long y) => (y & 0xFF) + S16(y) * 5 + (y & 0xFFFF) * 13 + S8(x) * 3 + (x & 0xFFFF) * 7 + (x & 0xFF) * 11;
    [MethodImpl(NI)] static long NarrowPointer(int* p) => (long)(byte)*p + (long)(sbyte)p[1] * 3 + (long)(short)p[2] * 5 + (long)(ushort)p[3] * 7 + (long)(p[4] & 0xFF) * 11 + (long)(p[5] & 0xFFFF) * 13;
    [MethodImpl(NI)] static long NarrowRaw(byte* p) => (long)(byte)Unsafe.ReadUnaligned<int>(p + 1) + (long)(short)Unsafe.ReadUnaligned<int>(p + 1) * 5 + (long)(Unsafe.ReadUnaligned<int>(p + 1) & 0xFFFF) * 13;
    [MethodImpl(NI)] static long NarrowMask(int i, int b) => (long)(Arr[i] & 0xFF00) + (long)(Arr[i] & 0x1FF) * 3 + (long)(Arr[i] & 0x7FFF) * 5 + (long)(Arr[i] & b & 0xFF) * 7;
    [MethodImpl(NI)] static bool NarrowCompare(int i, int b) => (byte)Arr[i] == (byte)b;
    [MethodImpl(NI)] static long NarrowStore(int i) { OutB[2] = (byte)Arr[i]; OutS[2] = (short)Arr[i]; OutS[3] = (short)(ushort)Arr[i]; return OutB[2] + OutS[2] * 3L + OutS[3] * 5L; }
    [MethodImpl(NI)] static long NarrowChecked(int i) { try { return checked((byte)Arr[i]); } catch (OverflowException) { return -3; } }

    // ---- other places an int is loaded from ----

    [MethodImpl(NI)]
    static long FromLocal(int a, int b)
    {
        int v = a;
        Touch(&v);          // v lives on the frame and is loaded at every use
        long r = (long)(v + b);
        r = r * 31 + (long)(v << (b & 31));
        r = r * 31 + (long)(v & 0x7FFFFFFF);
        r = r * 31 + (v == 100 ? 3 : 5);
        r = r * 31 + ((uint)v < 100u ? 7 : 11);
        r = r * 31 + (long)(byte)v + (long)(short)v;
        r = r * 31 + (long)v;
        r = r * 31 + (long)(uint)v;
        r = r * 31 + (v < b ? 13 : 17);
        r = r * 31 + (long)(v * b);
        return r;
    }

    [MethodImpl(NoOpt)]
    static long RFromLocal(long v, long b)
    {
        long r = S32(v + b);
        r = r * 31 + S32(v << (int)(b & 31));
        r = r * 31 + (v & 0x7FFFFFFF);
        r = r * 31 + (v == 100 ? 3 : 5);
        r = r * 31 + (Z32(v) < 100 ? 7 : 11);
        r = r * 31 + (v & 0xFF) + S16(v);
        r = r * 31 + v;
        r = r * 31 + Z32(v);
        r = r * 31 + (v < b ? 13 : 17);
        r = r * 31 + S32(v * b);
        return r;
    }

    [MethodImpl(NI)]
    static long FromStructLocal(int a, int b)
    {
        Pair p;
        p.X = a;
        p.Y = b;
        Touch(&p.Y);
        long r = (long)(p.X + p.Y);
        r = r * 31 + (long)(p.Y << 3);
        r = r * 31 + (long)(p.Y & 0x7FFFFFFF);
        r = r * 31 + (p.Y == 100 ? 3 : 5);
        r = r * 31 + ((uint)p.Y < 100u ? 7 : 11);
        r = r * 31 + (long)(byte)p.Y + (long)(short)p.Y;
        r = r * 31 + (long)p.Y;
        r = r * 31 + (long)(uint)p.X;
        return r;
    }

    [MethodImpl(NoOpt)]
    static long RFromStructLocal(long a, long b)
    {
        long r = S32(a + b);
        r = r * 31 + S32(b << 3);
        r = r * 31 + (b & 0x7FFFFFFF);
        r = r * 31 + (b == 100 ? 3 : 5);
        r = r * 31 + (Z32(b) < 100 ? 7 : 11);
        r = r * 31 + (b & 0xFF) + S16(b);
        r = r * 31 + b;
        r = r * 31 + Z32(a);
        return r;
    }

    [MethodImpl(NI)]
    static long FromField(Box o, int b)
    {
        long r = (long)(o.I + b);
        r = r * 31 + (long)(o.I - b);
        r = r * 31 + (long)(o.I & 0x7FFFFFFF);
        r = r * 31 + (o.I == 100 ? 3 : 5);
        r = r * 31 + ((uint)o.I < 100u ? 7 : 11);
        r = r * 31 + (long)o.U;
        r = r * 31 + (long)(int)o.U;
        r = r * 31 + (long)(o.U + (uint)b);
        r = r * 31 + (o.U == 100u ? 13 : 17);
        r = r * 31 + (o.U < 100u ? 19 : 23);
        r = r * 31 + (o.U > 0x7FFFFFFFu ? 29 : 31);
        r = r * 31 + (long)(o.U >> 1);
        r = r * 31 + (long)(o.U >> (b & 31));
        return r;
    }

    [MethodImpl(NoOpt)]
    static long RFromField(long v, long b)
    {
        long r = S32(v + b);
        r = r * 31 + S32(v - b);
        r = r * 31 + (v & 0x7FFFFFFF);
        r = r * 31 + (v == 100 ? 3 : 5);
        r = r * 31 + (Z32(v) < 100 ? 7 : 11);
        r = r * 31 + Z32(v);
        r = r * 31 + v;
        r = r * 31 + Z32(v + b);
        r = r * 31 + (Z32(v) == 100 ? 13 : 17);
        r = r * 31 + (Z32(v) < 100 ? 19 : 23);
        r = r * 31 + (Z32(v) > 0x7FFFFFFFL ? 29 : 31);
        r = r * 31 + (Z32(v) >> 1);
        r = r * 31 + (Z32(v) >> (int)(b & 31));
        return r;
    }

    // ---- a load kept across a call, and a load with two readers ----

    [MethodImpl(NI)] static long AcrossCall(int i, int b) => (long)(Arr[i] + Zero()) + (long)(Arr[i] & (Zero() + 0x7FFFFFFF)) * 3 + ((uint)Arr[i] < (uint)(Zero() + 100) ? 5 : 7);
    [MethodImpl(NoOpt)] static long RAcrossCall(long v) => v + (v & 0x7FFFFFFF) * 3 + (Z32(v) < 100 ? 5 : 7);

    [MethodImpl(NI)]
    static long TwoReaders(int i, int b)
    {
        int v = Arr[i];
        long r = (long)(v + b);         // a 32-bit reader
        r = r * 31 + v;                 // and a 64-bit one
        r = r * 31 + (v == 100 ? 3 : 5);
        r = r * 31 + (v < b ? 7 : 11);
        return r;
    }

    [MethodImpl(NoOpt)] static long RTwoReaders(long v, long b) => ((S32(v + b) * 31 + v) * 31 + (v == 100 ? 3 : 5)) * 31 + (v < b ? 7 : 11);

    [MethodImpl(NI)]
    static long InLoop(int[] a, int b)
    {
        long sum = 0;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] == 100) sum += 1000;
            if ((uint)a[i] < 100u) sum += 10;
            sum += a[i] & 0x7FFFFFFF;
            sum += (long)(a[i] + b) * 3;
            sum += (byte)a[i];
        }
        return sum;
    }

    [MethodImpl(NoOpt)]
    static long RInLoop(long b)
    {
        long sum = 0;
        for (int i = 0; i < N; i++)
        {
            long v = Source[i];
            if (v == 100) sum += 1000;
            if (Z32(v) < 100) sum += 10;
            sum += v & 0x7FFFFFFF;
            sum += S32(v + b) * 3;
            sum += v & 0xFF;
        }
        return sum;
    }

    // ---- zero extension of a value known not to be negative ----
    // Each method returns (ulong)(uint)x behind some fact about x; x is negative on some calls.

    [MethodImpl(NI)] static long ZxPlain(int x) => (long)(uint)x;
    [MethodImpl(NI)] static long ZxGuard(int x) => x >= 0 ? (long)(uint)x : (long)(uint)x + 1;
    [MethodImpl(NI)] static long ZxGuardNeg(int x) => x < 0 ? (long)(uint)x : -(long)(uint)x;
    [MethodImpl(NI)] static long ZxGuardUnsigned(int x) => (uint)x <= 0x7FFFFFFFu ? (long)(uint)x * 3 : (long)(uint)x;
    [MethodImpl(NI)] static long ZxGuardLe(int x, int n) => (uint)x < (uint)n ? (long)(uint)x * 3 : (long)(uint)x;
    [MethodImpl(NI)] static long ZxGuardLength(int x, int[] a) => (uint)x < (uint)a.Length ? (long)(uint)x * 3 + a[x] : (long)(uint)x;
    [MethodImpl(NI)] static long ZxAfterIndex(int x, long[] t) { try { long v = t[x]; return v + (long)(uint)x; } catch (IndexOutOfRangeException) { return (long)(uint)x; } }
    [MethodImpl(NI)] static long ZxChanged(int x, int d) { if (x < 0) return -1; x -= d; return (long)(uint)x; }
    [MethodImpl(NI)] static long ZxChangedAdd(int x, int d) { if (x < 0) return -1; x += d; return (long)(uint)x; }
    [MethodImpl(NI)] static long ZxChangedNeg(int x) { if (x <= 0) return -1; x = -x; return (long)(uint)x; }
    [MethodImpl(NI)] static long ZxChangedShift(int x) { if (x <= 0) return -1; x <<= 1; return (long)(uint)x; }
    [MethodImpl(NI)] static long ZxChangedNot(int x) { if (x < 0) return -1; x = ~x; return (long)(uint)x; }
    [MethodImpl(NI)] static long ZxMasked(int x) => (long)(uint)(x & 0x7FFFFFFF) + (long)(uint)(x & int.MinValue);
    [MethodImpl(NI)] static long ZxLength(int[] a, int d) => (long)(uint)a.Length + (long)(uint)(a.Length - d);
    [MethodImpl(NI)] static long ZxMod(int x, int n) => (long)(uint)(x % n) + (long)(uint)((uint)x % (uint)n);
    [MethodImpl(NI)] static long ZxAdd(long k, int x) => x >= 0 ? k + (long)(uint)x : k + (long)(uint)x + 1;
    [MethodImpl(NI)] static long ZxScale(long k, int x) => x >= 0 ? k + ((long)(uint)x << 3) : k + ((long)(uint)x << 2);
    [MethodImpl(NI)] static long ZxPointer(long* p, int x) => x >= 0 ? (long)((nint)(p + (uint)x) - (nint)p) : (long)((nint)(p + (uint)x) - (nint)p) + 1;
    [MethodImpl(NI)] static long ZxLoaded(int i) => Arr[i] >= 0 ? (long)(uint)Arr[i] : (long)(uint)Arr[i] + 1;
    [MethodImpl(NI)] static long ZxUintGuard(uint u) => u < 0x80000000u ? (long)u * 3 : (long)u;
    [MethodImpl(NI)] static long ZxUintGuardHigh(uint u) => u > 0x7FFFFFFFu ? (long)u * 3 : (long)u;
    [MethodImpl(NI)] static long ZxSwitch(int x) { switch (x) { case 0: case 1: case 2: case 3: return (long)(uint)x * 5; default: return (long)(uint)x; } }

    [MethodImpl(NI)]
    static long ZxLoop(int start, int steps)
    {
        // i starts non-negative and wraps around to negative values.
        long sum = 0;
        if (start < 0) return -1;
        int i = start;
        for (int n = 0; n < steps; n++)
        {
            sum += (long)(uint)i;
            i += 0x30000000;
        }
        return sum;
    }

    [MethodImpl(NoOpt)]
    static long RZxLoop(long start, long steps)
    {
        long sum = 0;
        if (start < 0) return -1;
        long i = start;
        for (long n = 0; n < steps; n++)
        {
            sum += Z32(i);
            i += 0x30000000;
        }
        return sum;
    }

    [MethodImpl(NI)]
    static long ZxLoopDown(int start)
    {
        // The loop ends when i becomes negative; the last value read is negative.
        long sum = 0;
        int i = start;
        while (i >= 0)
        {
            sum += (long)(uint)i;
            i -= 0x30000000;
        }
        return sum * 3 + (long)(uint)i;
    }

    [MethodImpl(NoOpt)]
    static long RZxLoopDown(long start)
    {
        long sum = 0;
        long i = start;
        while (i >= 0)
        {
            sum += Z32(i);
            i = S32(i - 0x30000000);
        }
        return sum * 3 + Z32(i);
    }

    [MethodImpl(NI)]
    static long ZxHandler(int x, int[] a)
    {
        int v = 5;
        try
        {
            if (x < 0) return -1;
            v = x;
            v = a[x] - 0x7FFFFFFF;      // may throw with v == x, may leave v negative
        }
        catch (IndexOutOfRangeException)
        {
            return (long)(uint)v + 1;
        }
        return (long)(uint)v;
    }

    [MethodImpl(NoOpt)]
    static long RZxHandler(long x, long len, long elem)
    {
        if (x < 0) return -1;
        if (x >= len) return Z32(x) + 1;
        return Z32(elem - 0x7FFFFFFF);
    }

    // ---- driver ----

    // The driver gets its 32-bit arguments from here, sign-extended whatever the 64-bit value was.
    [MethodImpl(NoOpt)] static int ToInt(long v) => (int)v;
    [MethodImpl(NoOpt)] static uint ToUInt(long v) => (uint)v;


    static readonly int[] Bs = { 0, 1, 2, 31, 32, 33, -1, 100, 0xFF, 0x1FF, int.MinValue, int.MaxValue, 16, -100, 0x12345678, 7 };

    static void Eq(string name, long a, long b, long want, long got)
    {
        checks++;
        if (want == got) return;
        fails++;
        if (++shown <= 40)
            Console.WriteLine("FAIL " + name + " (0x" + a.ToString("x") + ", 0x" + b.ToString("x") + ") expected 0x" + want.ToString("x")
                              + " got 0x" + got.ToString("x"));
    }

    static void Loads()
    {
        int[] len16 = new int[16];
        for (int i = 0; i < N; i++)
        {
            long v = Source[i];
            foreach (int b in Bs)
            {
                long lb = b;
                int sh = b & 31;
                Eq("load + int", v, lb, S32(v + lb), AddW(i, b));
                Eq("int - load", v, lb, S32(lb - v), SubW(i, b));
                Eq("load * int", v, lb, S32(v * lb), MulW(i, b));
                Eq("load << int", v, lb, S32(v << sh), ShlW(i, b));
                Eq("load >> int", v, lb, v >> sh, SarW(i, b));
                Eq("load >>> int", v, lb, S32(Z32(v) >> sh), ShrW(i, b));
                Eq("load >> 0", v, lb, v, Sar0(i, b));
                Eq("int shifted by load", v, lb, S32(lb << (int)(v & 31)) ^ (lb >> (int)(v & 31)), CountW(i, b));
                Eq("load & byte", v, lb, v & lb & 0xFF, AndByte(i, b));
                Eq("load & char", v, lb, (v & lb & 0xFFFF) * 2, AndChar(i, b));
                Eq("load & compare", v, lb, v & (lb < 7 ? 1 : 0), AndCompare(i, b));
                Eq("load & shifted", v, lb, v & (Z32(lb) >> 1), AndShr(i, b));
                Eq("load & int", v, lb, v & lb, AndInt(i, b));
                Eq("load | byte", v, lb, v | (lb & 0xFF), OrInt(i, b));
                Eq("load ^ byte", v, lb, v ^ (lb & 0xFF), XorInt(i, b));
                Eq("load / int", v, lb, v / ((lb & 0xFF) + 2), Div(i, b));
                Eq("int / load", v, lb, (lb >> 1) / S32(v | 0x40000000), DivBy(i, b));
                Eq("load % int", v, lb, v % ((lb & 0xFF) + 2), Mod(i, b));
                Eq("load / uint", v, lb, Z32(v) / ((lb & 0xFF) + 2), UDiv(i, b));
                Eq("load == byte", v, lb, B(v == (lb & 0xFF)), B(EqByte(i, b)));
                Eq("char == load", v, lb, B(v == (lb & 0xFFFF)), B(EqChar(i, b)));
                Eq("load <u byte", v, lb, B(Z32(v) < (lb & 0xFF)), B(UltByte(i, b)));
                Eq("load >u masked", v, lb, B(Z32(v) > (lb & 0x7FFFFFFF)), B(UgtMasked(i, b)));
                Eq("load == masked", v, lb, B(v == (lb & 0x7FFFFFFF)), B(EqMasked(i, b)));
                Eq("load == int", v, lb, B(v == lb), B(EqInt(i, b)));
                Eq("load <u int", v, lb, B(Z32(v) < Z32(lb)), B(UltInt(i, b)));
                Eq("load == compare", v, lb, B(v == (lb < 7 ? 1 : 0)), B(EqCompare(i, b)));
                Eq("branches", v, lb, RBranches(v, lb), Branches(i, b));
                Eq("narrow mask", v, lb, (v & 0xFF00) + (v & 0x1FF) * 3 + (v & 0x7FFF) * 5 + (v & lb & 0xFF) * 7, NarrowMask(i, b));
                Eq("narrow compare", v, lb, B((v & 0xFF) == (lb & 0xFF)), B(NarrowCompare(i, b)));
                Eq("two readers", v, lb, RTwoReaders(v, lb), TwoReaders(i, b));
                Eq("local on the frame", v, lb, RFromLocal(v, lb), FromLocal(ToInt(v), b));
                Eq("struct local on the frame", v, lb, RFromStructLocal(lb, v), FromStructLocal(b, ToInt(v)));
                Obj.I = (int)v; Obj.U = (uint)v;
                Eq("object field", v, lb, RFromField(v, lb), FromField(Obj, b));
                Eq("load across a call", v, lb, RAcrossCall(v), AcrossCall(i, b));
            }

            Eq("-load", v, 0, S32(-v), NegW(i, 0));
            Eq("load >> 3", v, 0, v >> 3, SarC(i, 0));
            Eq("load >>> 1", v, 0, Z32(v) >> 1, ShrZ(i, 0));
            Eq("load & max", v, 0, v & 0x7FFFFFFF, AndMax(i, 0));
            Eq("load & small", v, 0, v & 0x7F0, AndSmall(i, 0));
            Eq("load & negative", v, 0, S32(v & 0x80000001L), AndNeg(i, 0));
            Eq("load & length", v, 0, v & 16, AndLength(i, len16));
            Eq("load == 100", v, 0, B(v == 100), B(EqConst(i)));
            Eq("load != max", v, 0, B(v != 0x7FFFFFFF), B(NeConst(i)));
            Eq("load == 0", v, 0, B(v == 0), B(EqZero(i)));
            Eq("load == -1", v, 0, B(v == -1), B(EqNegConst(i)));
            Eq("load == min", v, 0, B(v == -0x80000000L), B(EqMin(i)));
            Eq("load <u 100", v, 0, B(Z32(v) < 100), B(UltConst(i)));
            Eq("load >u max", v, 0, B(Z32(v) > 0x7FFFFFFFL), B(UgtConst(i)));
            Eq("load >=u max", v, 0, B(Z32(v) >= 0x7FFFFFFFL), B(UgeConst(i)));
            Eq("load <u 2^31", v, 0, B(Z32(v) < 0x80000000L), B(UltBig(i)));
            Eq("load <u 2^32-1", v, 0, B(Z32(v) < 0xFFFFFFFFL), B(UltHuge(i)));
            Eq("load < 100", v, 0, B(v < 100), B(LtConst(i)));
            Eq("load > 100", v, 0, B(v > 100), B(GtConst(i)));
            Eq("load < 0", v, 0, B(v < 0), B(LtZero(i)));
            Eq("load >= 0", v, 0, B(v >= 0), B(GeZero(i)));
            Eq("load == length", v, 0, B(v == 16), B(EqLength(i, len16)));
            Eq("load <u length", v, 0, B(Z32(v) < 16), B(UltLength(i, len16)));
            Eq("index loaded", v, 0, v >= 0 && v < 16 ? Tab[v] : -2, IndexLoaded(i));
            Eq("index loaded, span", v, 0, v >= 0 && v < 8 ? v * 3 + 1 : -2, IndexSpan(i));
            Eq("index loaded, constant length", v, 0, v >= 0 && v < 8 ? 11 + v : -2, IndexConstLength(i));
            Eq("index loaded and guarded", v, 0, v >= 0 && v < 16 ? Tab[v] : -2, IndexGuarded(i));
            Eq("length loaded", v, 0, v >= 0 && v < N ? Source[v] : -2, LengthLoaded(Arr, ToInt(v)));
            Eq("span length loaded", v, 0, v >= 0 && v < 5 ? Source[v + 2] : -2, SpanLength(new Span<int>(Arr, 2, 5), ToInt(v)));
            Eq("stores", v, 0, RStores(v), Stores(i));
            Eq("store to a struct local", v, 0, v * 3 + S32(v + 1), StoreLocalField(i));
            Eq("narrowed loads", v, 0, RNarrow(v), Narrow(i));
            Eq("narrowed uint loads", v, 0, RNarrow(v), NarrowU(i));
            Obj.I = (int)v;
            Eq("narrowed field loads", v, 0, RNarrow(v), NarrowField(Obj));
            Eq("narrowed struct loads", v, 0, RNarrowPair(v, ~v), NarrowPair(i));
            Eq("narrowed stores", v, 0, (v & 0xFF) + S16(v) * 3 + S16(v) * 5, NarrowStore(i));
            Eq("narrowed checked", v, 0, v >= 0 && v <= 255 ? v : -3, NarrowChecked(i));
            Eq("length uses", v, 0, 16 * 3 + (16 < v ? 5 : 7) + (Z32(v) < 16 ? 11 : 13) + 16 + S32(16 - v), LengthUses(len16, ToInt(v)));
            Eq("shifted by load", v, 0x123456789ABCDEF0L, (0x123456789ABCDEF0L << (int)(v & 63)) ^ (0x123456789ABCDEF0L >> (int)(v & 63)) ^ (long)(0x123456789ABCDEF0UL >> (int)(v & 63)), Count(i, 0x123456789ABCDEF0L));
            Eq("widened load + long", v, 0, v + 0x100000000L, AddWiden(i, 0x100000000L));
            Eq("widened uint load + long", v, 0, Z32(v) + 0x100000000L, AddWidenU(i, 0x100000000L));

            for (int j = 0; j < N; j++)
            {
                long w = Source[j];
                Eq("load + load", v, w, S32(v + w), AddTwo(i, j));
                Eq("load * load, widened", v, w, v * w, MulWiden(i, j));
                Eq("load == load", v, w, B(v == w), B(EqTwo(i, j)));
                Eq("load <u load", v, w, B(Z32(v) < Z32(w)), B(UltTwo(i, j)));
            }
        }

        fixed (int* p = Arr)
        {
            for (int i = 0; i + 6 <= N; i++)
            {
                long want = (Source[i] & 0xFF) + S8(Source[i + 1]) * 3 + S16(Source[i + 2]) * 5 + (Source[i + 3] & 0xFFFF) * 7
                            + (Source[i + 4] & 0xFF) * 11 + (Source[i + 5] & 0xFFFF) * 13;
                Eq("narrowed pointer loads", i, 0, want, NarrowPointer(p + i));
            }
        }

        byte* raw = stackalloc byte[16];
        for (int i = 0; i < N; i++)
        {
            long v = Source[i];
            for (int j = 0; j < 4; j++) raw[1 + j] = (byte)(v >> (8 * j));
            Eq("narrowed unaligned loads", v, 0, (v & 0xFF) + S16(v) * 5 + (v & 0xFFFF) * 13, NarrowRaw(raw));
        }

        foreach (int b in Bs)
            Eq("loads in a loop", b, 0, RInLoop(b), InLoop(Arr, b));
    }

    [MethodImpl(NoOpt)] static long Sel(bool f, long a, long b) => f ? a : b;

    static void ZeroExtensions()
    {
        long[] t = Tab;
        int[] a8 = new int[8];
        for (int i = 0; i < 8; i++) a8[i] = i * 0x20000000;
        long* mem = stackalloc long[1];
        for (int i = 0; i < N; i++)
        {
            long v = Source[i];
            int x = ToInt(v);
            long z = Z32(v);
            Eq("zext", v, 0, z, ZxPlain(x));
            Eq("zext behind x >= 0", v, 0, Sel(v >= 0, z, z + 1), ZxGuard(x));
            Eq("zext behind x < 0", v, 0, Sel(v < 0, z, -z), ZxGuardNeg(x));
            Eq("zext behind an unsigned compare", v, 0, Sel(z <= 0x7FFFFFFFL, z * 3, z), ZxGuardUnsigned(x));
            Eq("zext behind a length check", v, 0, Sel(z < 8, z * 3 + S32((v & 7) * 0x20000000), z), ZxGuardLength(x, a8));
            Eq("zext after an index", v, 0, Sel(v >= 0 && v < 16, t[v & 15] + z, z), ZxAfterIndex(x, t));
            Eq("zext after x = -x", v, 0, Sel(v <= 0, -1, Z32(-v)), ZxChangedNeg(x));
            Eq("zext after x <<= 1", v, 0, Sel(v <= 0, -1, Z32(v << 1)), ZxChangedShift(x));
            Eq("zext after x = ~x", v, 0, Sel(v < 0, -1, Z32(~v)), ZxChangedNot(x));
            Eq("zext of masked values", v, 0, (v & 0x7FFFFFFF) + (z & 0x80000000L), ZxMasked(x));
            Eq("zext + long", v, 0, Sel(v >= 0, 5 + z, 6 + z), ZxAdd(5, x));
            Eq("zext scaled + long", v, 0, Sel(v >= 0, 5 + (z << 3), 5 + (z << 2)), ZxScale(5, x));
            Eq("zext in pointer arithmetic", v, 0, Sel(v >= 0, (long)(nint)(z * 8), (long)(nint)(z * 8) + 1), ZxPointer(mem, x));
            Eq("zext of a load", v, 0, Sel(v >= 0, z, z + 1), ZxLoaded(i));
            Eq("zext behind u < 2^31", v, 0, Sel(z < 0x80000000L, z * 3, z), ZxUintGuard(ToUInt(v)));
            Eq("zext behind u > 2^31-1", v, 0, Sel(z > 0x7FFFFFFFL, z * 3, z), ZxUintGuardHigh(ToUInt(v)));
            Eq("zext in a switch", v, 0, Sel(v >= 0 && v <= 3, z * 5, z), ZxSwitch(x));
            Eq("zext in a loop that wraps", v, 0, RZxLoop(v, 9), ZxLoop(x, 9));
            Eq("zext after a loop", v, 0, RZxLoopDown(v), ZxLoopDown(x));
            Eq("zext in a handler", v, 0, RZxHandler(v, 8, (v & 7) * 0x20000000), ZxHandler(x, a8));

            foreach (int d in Bs)
            {
                long ld = d;
                Eq("zext behind x <u n", v, ld, Sel(z < Z32(ld), z * 3, z), ZxGuardLe(x, d));
                Eq("zext after x -= d", v, ld, Sel(v < 0, -1, Z32(v - ld)), ZxChanged(x, d));
                Eq("zext after x += d", v, ld, Sel(v < 0, -1, Z32(v + ld)), ZxChangedAdd(x, d));
                Eq("zext of length - d", v, ld, 8 + Z32(8 - ld), ZxLength(a8, d));
                if (d != 0 && !(d == -1 && x == int.MinValue))
                    Eq("zext of remainders", v, ld, Z32(v % ld) + z % Z32(ld), ZxMod(x, d));
            }
        }
    }

    static int Main()
    {
        Setup();
        Loads();
        ZeroExtensions();
        Console.WriteLine(fails == 0 ? "ALL PASS (" + checks + " checks)" : "FAILED " + fails + " of " + checks);
        return fails == 0 ? 0 : 1;
    }
}
