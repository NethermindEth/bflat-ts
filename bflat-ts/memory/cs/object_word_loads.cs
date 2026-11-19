// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// A 32-bit field of an object loaded with an 8-byte load.
//
// On a zkVM target (JitZkBumpAddr set) an int that sits at an 8-byte aligned
// offset of an object is loaded together with the four bytes that follow it
// when the one user of the load never reads the upper half of the register:
// a store of four bytes or less, a 32-bit add, subtract, multiply, negate or
// shift, a shift count, a cast to a small type, an AND with a non-negative
// value. The upper half of the register then holds the neighbouring field.
// What can go wrong: a user that does read the upper half and gets such a
// load anyway (a widening to long, a compare, a division, an overflow check,
// an index, an argument, a return value, an OR, XOR or NOT, an AND with a
// value that may be negative), a result that is not a proper 32-bit value
// afterwards, a load of eight bytes where only four belong to the object,
// and a store that writes the neighbour.
//
// Every int here has a neighbour in the upper half of its word that is all
// ones, the largest or smallest int or a pattern, never only the sign
// extension of the int. The ints are fields at offsets 8, 16 and 40 of an
// object (the last one ends the object), fields of a struct inside an
// object, elements 0, 2 and 4 of an int array, a boxed int, and the length
// of a string. Each is used in every way listed above, and every result is
// widened to long so that a wrong upper half shows.
//
// The expected values are computed from the same ints passed as arguments to
// a method compiled without optimization. Each group prints one line; the
// run ends with ALL PASS or a FAIL count.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit)]
class Words
{
    [FieldOffset(0)] public int A;
    [FieldOffset(4)] public int AHigh;
    [FieldOffset(8)] public int B;
    [FieldOffset(12)] public int BHigh;
    [FieldOffset(16)] public long L;
    [FieldOffset(24)] public short S0;
    [FieldOffset(26)] public short S1;
    [FieldOffset(28)] public int SHigh;
    [FieldOffset(32)] public int Last;
}

struct Pair { public int Low; public int High; }

class Holder
{
    public long Before;
    public Pair P;
    public Pair Q;
    public long After;
}

static unsafe class Ops
{
    public const long Threw = 0x5EAD5EAD5EADL;

    // ---- users that never read the upper half ----

    [MethodImpl(MethodImplOptions.NoInlining)] public static long Add(Words w, int x) => w.A + x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Sub(Words w, int x) => w.A - x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long SubFrom(Words w, int x) => x - w.A;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Mul(Words w, int x) => w.A * x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Neg(Words w, int x) => -w.A;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Shl(Words w, int x) => w.A << x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Sar(Words w, int x) => w.A >> x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Shr(Words w, int x) => (int)((uint)w.A >> x);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long ShrUnsigned(Words w, int x) => (uint)w.A >> x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Count(Words w, int x) => x << w.A;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long CountSar(Words w, int x) => x >> w.A;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long CountLong(Words w, int x) => (x * 0x100000001L) << w.A;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long CountLongShr(Words w, int x) => (long)((ulong)(x * 0x100000001L) >> w.A);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long AndConst(Words w, int x) => w.A & 0x7FFF00F0;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long AndLowBits(Words w, int x) => w.A & 0x3FF;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long AndNonNegative(Words w, int x) => w.A & (x & 0x7FFFFFFF);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long AndByte(Words w, int x) => w.A & (byte)x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long AndLength(Words w, int[] a) => w.A & a.Length;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long ToByte(Words w, int x) => (byte)w.A;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long ToSByte(Words w, int x) => (sbyte)w.A;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long ToShort(Words w, int x) => (short)w.A;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long ToUShort(Words w, int x) => (ushort)w.A;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long AddOne(Words w, int x) => w.A + 1;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long SubOne(Words w, int x) => w.A - 1;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long MulThree(Words w, int x) => w.A * 3;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long MulEight(Words w, int x) => w.A * 8;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long MulBig(Words w, int x) => w.A * 0x10001;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long ShlThree(Words w, int x) => w.A << 3;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long SarThree(Words w, int x) => w.A >> 3;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Sar31(Words w, int x) => w.A >> 31;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long ShrThree(Words w, int x) => (uint)w.A >> 3;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Shr31(Words w, int x) => (uint)w.A >> 31;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long TwoFields(Words w, int x) => w.A + w.B;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long TwoFieldsMul(Words w, int x) => w.A * w.B - w.Last;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Chain(Words w, int x) => (w.A + x) * (w.B - x);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long SameTwice(Words w, int x) => w.A * w.A + x;

    // The result of the 32-bit operation is used as a 64-bit value in other ways than a widening.
    [MethodImpl(MethodImplOptions.NoInlining)] public static long AddThenCompare(Words w, int x) => (w.A + x < 0) ? 1 : 2;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long AddThenDivide(Words w, int x) => (w.A + x) / 3;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long AddThenIndex(Words w, long[] t) => t[(w.A + 5) & 63];
    [MethodImpl(MethodImplOptions.NoInlining)] public static long AddThenUnsigned(Words w, int x) => (uint)(w.A + x);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long ShlThenLong(Words w, int x) => (long)(w.A << 4) * 0x100000001L;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long NegThenShift(Words w, int x) => (long)(-w.A) << 20;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long AddThenCall(Words w, int x) => Widen(w.A + x);

    // ---- stores of four bytes or less ----

    [MethodImpl(MethodImplOptions.NoInlining)] public static void StoreField(Words d, Words w) => d.B = w.A;
    [MethodImpl(MethodImplOptions.NoInlining)] public static void StoreLast(Words d, Words w) => d.Last = w.A;
    [MethodImpl(MethodImplOptions.NoInlining)] public static void StoreShort(Words d, Words w) => d.S0 = (short)w.A;
    [MethodImpl(MethodImplOptions.NoInlining)] public static void StoreElement(int[] d, int i, Words w) => d[i] = w.A;
    [MethodImpl(MethodImplOptions.NoInlining)] public static void StoreConstElement(int[] d, Words w) => d[2] = w.A;
    [MethodImpl(MethodImplOptions.NoInlining)] public static void StorePointer(int* d, Words w) => *d = w.A;
    [MethodImpl(MethodImplOptions.NoInlining)] public static void StoreBytePointer(byte* d, Words w) => *d = (byte)w.A;
    [MethodImpl(MethodImplOptions.NoInlining)] public static void StoreRef(ref int d, Words w) => d = w.A;
    [MethodImpl(MethodImplOptions.NoInlining)] public static void StoreStruct(Holder d, Words w) => d.P.Low = w.A;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long StoreLocal(Words w, int x)
    {
        Pair p;
        p.High = x;
        p.Low = w.A;
        Touch(&p);
        return ((long)p.High << 32) ^ p.Low;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long StoreMany(Words w, int[] d)
    {
        d[0] = w.A;
        d[1] = w.B;
        d[2] = w.Last;
        d[3] = w.A + w.B;
        return d.Length;
    }

    // ---- users that read the upper half ----

    [MethodImpl(MethodImplOptions.NoInlining)] public static long ToLong(Words w, int x) => w.A;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long ToULong(Words w, int x) => (long)(ulong)(uint)w.A;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Equal(Words w, int x) => (w.A == x) ? 1 : 2;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Less(Words w, int x) => (w.A < x) ? 1 : 2;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long LessUnsigned(Words w, int x) => ((uint)w.A < (uint)x) ? 1 : 2;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long IsZero(Words w, int x) => (w.A == 0) ? 1 : 2;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long IsNegative(Words w, int x) => (w.A < 0) ? 1 : 2;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long IsPositive(Words w, int x) => (w.A > 0) ? 1 : 2;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long LessConst(Words w, int x) => (w.A < 1000) ? 1 : 2;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long EqualLong(Words w, int x) => (w.A == x * 0x100000001L) ? 1 : 2;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Div(Words w, int x) => w.A / x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Rem(Words w, int x) => w.A % x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long DivBy(Words w, int x) => x / w.A;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long DivUnsigned(Words w, int x) => (uint)w.A / (uint)x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long DivConst(Words w, int x) => w.A / 7;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long RemConst(Words w, int x) => w.A % 16;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Or(Words w, int x) => w.A | x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Xor(Words w, int x) => w.A ^ x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Not(Words w, int x) => ~w.A;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long And(Words w, int x) => w.A & x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long AndNegative(Words w, int x) => w.A & -256;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long AndLong(Words w, int x) => w.A & (x * 0x100000001L);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long LongShl(Words w, int x) => (long)w.A << 3;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long LongMul(Words w, int x) => w.A * (x * 0x100000001L);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long LongAdd(Words w, int x) => x * 0x100000001L + w.A;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long UnsignedMul(Words w, int x) => (long)((ulong)(uint)w.A * 3);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Argument(Words w, int x) => Widen(w.A);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long ArgumentSixth(Words w, int x) => Sixth(x, x, x, x, x, w.A);
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Return(Words w) => w.A;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Index(Words w, long[] t) => t[w.A];
    [MethodImpl(MethodImplOptions.NoInlining)] public static long IndexMasked(Words w, long[] t) => t[w.A & 63];
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Select(Words w, int x) => (x > 0) ? w.A : w.B;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Max(Words w, int x) => Math.Max(w.A, x);
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Abs(Words w, int x) => (w.A < 0) ? -(long)w.A : w.A;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Switch(Words w, int x)
    {
        switch (w.A)
        {
            case 0: return x + 10;
            case 1: return x + 21;
            case 2: return x + 32;
            case 3: return x + 43;
            case 4: return x + 54;
            case 255: return x + 65;
            default: return x - 1;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long CheckedAdd(Words w, int x)
    {
        try { return checked(w.A + x); } catch (OverflowException) { return Threw; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long CheckedSub(Words w, int x)
    {
        try { return checked(w.A - x); } catch (OverflowException) { return Threw; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long CheckedMul(Words w, int x)
    {
        try { return checked(w.A * x); } catch (OverflowException) { return Threw; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long CheckedUnsignedAdd(Words w, int x)
    {
        try { return checked(unchecked((uint)w.A) + unchecked((uint)x)); } catch (OverflowException) { return Threw; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long CheckedToShort(Words w, int x)
    {
        try { return checked((short)w.A); } catch (OverflowException) { return Threw; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long CheckedToByte(Words w, int x)
    {
        try { return checked((byte)w.A); } catch (OverflowException) { return Threw; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long CheckedToUInt(Words w, int x)
    {
        try { return checked((uint)w.A); } catch (OverflowException) { return Threw; }
    }

    // A loop: the load sits in the body, the field does not change.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long LoopSum(Words w, int x)
    {
        long sum = 0;
        for (int i = 0; i < 5; i++)
            sum += (w.A + i) ^ x;
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)] static long Widen(int v) => v;
    [MethodImpl(MethodImplOptions.NoInlining)] static long Sixth(int a, int b, int c, int d, int e, int f) => (long)f * 3 + a + b + c + d + e;
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)] static void Touch(void* p) { }

    // ---- other ints at an 8-byte aligned offset of an object ----

    [MethodImpl(MethodImplOptions.NoInlining)] public static long LastAdd(Words w, int x) => w.Last + x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long LastShl(Words w, int x) => w.Last << x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long LastLong(Words w, int x) => w.Last;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long LastLess(Words w, int x) => (w.Last < x) ? 1 : 2;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long SecondAdd(Words w, int x) => w.B + x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long SecondMul(Words w, int x) => w.B * x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long SecondLong(Words w, int x) => w.B;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long SecondDiv(Words w, int x) => w.B / x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long ShortAdd(Words w, int x) => w.S0 + x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long ShortLong(Words w, int x) => w.S0;

    [MethodImpl(MethodImplOptions.NoInlining)] public static long StructAdd(Holder h, int x) => h.P.Low + x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long StructSub(Holder h, int x) => h.Q.Low - x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long StructLong(Holder h, int x) => h.P.Low;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long StructLess(Holder h, int x) => (h.Q.Low < x) ? 1 : 2;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long StructHighAdd(Holder h, int x) => h.P.High + x;

    [MethodImpl(MethodImplOptions.NoInlining)] public static long Element0Add(int[] a, int x) => a[0] + x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Element2Mul(int[] a, int x) => a[2] * x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Element4Shl(int[] a, int x) => a[4] << x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Element1Add(int[] a, int x) => a[1] + x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Element0Long(int[] a, int x) => a[0];
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Element2Less(int[] a, int x) => (a[2] < x) ? 1 : 2;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long Element4Div(int[] a, int x) => a[4] / x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long ElementsSum(int[] a, int x) => a[0] + a[2] + a[4] + x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long ElementAt(int[] a, int i, int x) => a[i] + x;

    [MethodImpl(MethodImplOptions.NoInlining)] public static long BoxedAdd(object o, int x) => (int)o + x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long BoxedLong(object o, int x) => (int)o;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long BoxedLess(object o, int x) => ((int)o < x) ? 1 : 2;

    [MethodImpl(MethodImplOptions.NoInlining)] public static long StringLengthAdd(string s, int x) => s.Length + x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long StringLengthShl(string s, int x) => s.Length << x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long StringLengthMul(string s, int x) => s.Length * x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long StringLengthLong(string s, int x) => (long)s.Length << 31;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long ArrayLengthAdd(short[] a, int x) => a.Length + x;
    [MethodImpl(MethodImplOptions.NoInlining)] public static long ArrayLengthSub(short[] a, int x) => x - a.Length;
}

static unsafe class Program
{
    const MethodImplOptions Plain = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;
    const int OpCount = 86;

    static int s_fails;
    static int s_groupFails;
    static readonly long[] s_table = new long[64];
    static readonly int[] s_len37 = new int[37];

    static readonly int[] s_values =
    {
        0, 1, -1, int.MinValue, int.MaxValue, 0x12345678, unchecked((int)0x87654321), 255, 256, -256, 65535,
        0x7FFF0000, 2, 3, 4, 31, 32, 33, -32768, 0x00010000,
    };

    static readonly int[] s_high = { -1, int.MaxValue, int.MinValue, 0x5A5A5A5A, 0 };
    static readonly int[] s_x = { 0, 1, -1, 3, 7, 31, 32, 33, int.MaxValue, int.MinValue, 0x10001, -7, 1000 };

    static void EndGroup(string name)
    {
        if (s_groupFails == 0)
            Console.WriteLine("ok   " + name);
        else
            Console.WriteLine("FAIL " + name + " cases=" + s_groupFails);
        s_fails += s_groupFails;
        s_groupFails = 0;
    }

    static void Check(string name, int f, int high, int x, long expected, long got)
    {
        if (expected == got)
            return;
        s_groupFails++;
        if (s_groupFails <= 12)
        {
            Console.WriteLine("FAIL " + name + " field 0x" + f.ToString("x8") + " neighbour 0x" + high.ToString("x8") + " x " + x +
                              " expected " + expected + " got " + got);
        }
    }

    static string Name(int op) => op switch
    {
        0 => "Add", 1 => "Sub", 2 => "SubFrom", 3 => "Mul", 4 => "Neg", 5 => "Shl", 6 => "Sar", 7 => "Shr",
        8 => "ShrUnsigned", 9 => "Count", 10 => "CountSar", 11 => "CountLong", 12 => "CountLongShr",
        13 => "AndConst", 14 => "AndLowBits", 15 => "AndNonNegative", 16 => "AndByte", 17 => "AndLength",
        18 => "ToByte", 19 => "ToSByte", 20 => "ToShort", 21 => "ToUShort", 22 => "AddOne", 23 => "SubOne",
        24 => "MulThree", 25 => "MulEight", 26 => "MulBig", 27 => "ShlThree", 28 => "SarThree", 29 => "Sar31",
        30 => "ShrThree", 31 => "Shr31", 32 => "TwoFields", 33 => "TwoFieldsMul", 34 => "Chain", 35 => "SameTwice",
        36 => "AddThenCompare", 37 => "AddThenDivide", 38 => "AddThenIndex", 39 => "AddThenUnsigned",
        40 => "ShlThenLong", 41 => "NegThenShift", 42 => "AddThenCall", 43 => "StoreLocal", 44 => "ToLong",
        45 => "ToULong", 46 => "Equal", 47 => "Less", 48 => "LessUnsigned", 49 => "IsZero", 50 => "IsNegative",
        51 => "IsPositive", 52 => "LessConst", 53 => "EqualLong", 54 => "Div", 55 => "Rem", 56 => "DivBy",
        57 => "DivUnsigned", 58 => "DivConst", 59 => "RemConst", 60 => "Or", 61 => "Xor", 62 => "Not", 63 => "And",
        64 => "AndNegative", 65 => "AndLong", 66 => "LongShl", 67 => "LongMul", 68 => "LongAdd", 69 => "UnsignedMul",
        70 => "Argument", 71 => "ArgumentSixth", 72 => "Return", 73 => "Index", 74 => "IndexMasked", 75 => "Select",
        76 => "Max", 77 => "Abs", 78 => "Switch", 79 => "CheckedAdd", 80 => "CheckedSub", 81 => "CheckedMul",
        82 => "CheckedUnsignedAdd", 83 => "CheckedToShort", 84 => "CheckedToByte", 85 => "CheckedToUInt",
        _ => "LoopSum",
    };

    // Whether the operation is defined for these operands (no division by zero, no index out of range).
    [MethodImpl(Plain)]
    static bool Defined(int op, int f, int b, int x)
    {
        switch (op)
        {
            case 54: case 55: return (x != 0) && !((f == int.MinValue) && (x == -1));
            case 56: return (f != 0) && !((x == int.MinValue) && (f == -1));
            case 57: return x != 0;
            case 73: return (f >= 0) && (f < 64);
            default: return true;
        }
    }

    // The expected value, from the ints themselves.
    [MethodImpl(Plain)]
    static long Expected(int op, int f, int b, int last, int x)
    {
        long lx = x * 0x100000001L;
        switch (op)
        {
            case 0: return f + x;
            case 1: return f - x;
            case 2: return x - f;
            case 3: return f * x;
            case 4: return -f;
            case 5: return f << x;
            case 6: return f >> x;
            case 7: return (int)((uint)f >> x);
            case 8: return (uint)f >> x;
            case 9: return x << f;
            case 10: return x >> f;
            case 11: return lx << f;
            case 12: return (long)((ulong)lx >> f);
            case 13: return f & 0x7FFF00F0;
            case 14: return f & 0x3FF;
            case 15: return f & (x & 0x7FFFFFFF);
            case 16: return f & (byte)x;
            case 17: return f & 37;
            case 18: return (byte)f;
            case 19: return (sbyte)f;
            case 20: return (short)f;
            case 21: return (ushort)f;
            case 22: return f + 1;
            case 23: return f - 1;
            case 24: return f * 3;
            case 25: return f * 8;
            case 26: return f * 0x10001;
            case 27: return f << 3;
            case 28: return f >> 3;
            case 29: return f >> 31;
            case 30: return (uint)f >> 3;
            case 31: return (uint)f >> 31;
            case 32: return f + b;
            case 33: return f * b - last;
            case 34: return (f + x) * (b - x);
            case 35: return f * f + x;
            case 36: return (f + x < 0) ? 1 : 2;
            case 37: return (f + x) / 3;
            case 38: return s_table[(f + 5) & 63];
            case 39: return (uint)(f + x);
            case 40: return (long)(f << 4) * 0x100000001L;
            case 41: return (long)(-f) << 20;
            case 42: return f + x;
            case 43: return ((long)x << 32) ^ f;
            case 44: return f;
            case 45: return (long)(ulong)(uint)f;
            case 46: return (f == x) ? 1 : 2;
            case 47: return (f < x) ? 1 : 2;
            case 48: return ((uint)f < (uint)x) ? 1 : 2;
            case 49: return (f == 0) ? 1 : 2;
            case 50: return (f < 0) ? 1 : 2;
            case 51: return (f > 0) ? 1 : 2;
            case 52: return (f < 1000) ? 1 : 2;
            case 53: return (f == lx) ? 1 : 2;
            case 54: return f / x;
            case 55: return f % x;
            case 56: return x / f;
            case 57: return (uint)f / (uint)x;
            case 58: return f / 7;
            case 59: return f % 16;
            case 60: return f | x;
            case 61: return f ^ x;
            case 62: return ~f;
            case 63: return f & x;
            case 64: return f & -256;
            case 65: return f & lx;
            case 66: return (long)f << 3;
            case 67: return f * lx;
            case 68: return lx + f;
            case 69: return (long)((ulong)(uint)f * 3);
            case 70: return f;
            case 71: return (long)f * 3 + 5L * x;
            case 72: return f;
            case 73: return s_table[f];
            case 74: return s_table[f & 63];
            case 75: return (x > 0) ? f : b;
            case 76: return (f > x) ? f : x;
            case 77: return (f < 0) ? -(long)f : f;
            case 78:
                switch (f)
                {
                    case 0: return x + 10;
                    case 1: return x + 21;
                    case 2: return x + 32;
                    case 3: return x + 43;
                    case 4: return x + 54;
                    case 255: return x + 65;
                    default: return x - 1;
                }
            case 79: { long r = (long)f + x; return (r == (int)r) ? r : Ops.Threw; }
            case 80: { long r = (long)f - x; return (r == (int)r) ? r : Ops.Threw; }
            case 81: { long r = (long)f * x; return (r == (int)r) ? r : Ops.Threw; }
            case 82: { long r = (long)(uint)f + (uint)x; return (r == (uint)r) ? r : Ops.Threw; }
            case 83: return (f == (short)f) ? f : Ops.Threw;
            case 84: return (f == (byte)f) ? f : Ops.Threw;
            case 85: return (f >= 0) ? f : Ops.Threw;
            default:
            {
                long sum = 0;
                for (int i = 0; i < 5; i++)
                    sum += (f + i) ^ x;
                return sum;
            }
        }
    }

    static long Call(int op, Words w, int x)
    {
        switch (op)
        {
            case 0: return Ops.Add(w, x);
            case 1: return Ops.Sub(w, x);
            case 2: return Ops.SubFrom(w, x);
            case 3: return Ops.Mul(w, x);
            case 4: return Ops.Neg(w, x);
            case 5: return Ops.Shl(w, x);
            case 6: return Ops.Sar(w, x);
            case 7: return Ops.Shr(w, x);
            case 8: return Ops.ShrUnsigned(w, x);
            case 9: return Ops.Count(w, x);
            case 10: return Ops.CountSar(w, x);
            case 11: return Ops.CountLong(w, x);
            case 12: return Ops.CountLongShr(w, x);
            case 13: return Ops.AndConst(w, x);
            case 14: return Ops.AndLowBits(w, x);
            case 15: return Ops.AndNonNegative(w, x);
            case 16: return Ops.AndByte(w, x);
            case 17: return Ops.AndLength(w, s_len37);
            case 18: return Ops.ToByte(w, x);
            case 19: return Ops.ToSByte(w, x);
            case 20: return Ops.ToShort(w, x);
            case 21: return Ops.ToUShort(w, x);
            case 22: return Ops.AddOne(w, x);
            case 23: return Ops.SubOne(w, x);
            case 24: return Ops.MulThree(w, x);
            case 25: return Ops.MulEight(w, x);
            case 26: return Ops.MulBig(w, x);
            case 27: return Ops.ShlThree(w, x);
            case 28: return Ops.SarThree(w, x);
            case 29: return Ops.Sar31(w, x);
            case 30: return Ops.ShrThree(w, x);
            case 31: return Ops.Shr31(w, x);
            case 32: return Ops.TwoFields(w, x);
            case 33: return Ops.TwoFieldsMul(w, x);
            case 34: return Ops.Chain(w, x);
            case 35: return Ops.SameTwice(w, x);
            case 36: return Ops.AddThenCompare(w, x);
            case 37: return Ops.AddThenDivide(w, x);
            case 38: return Ops.AddThenIndex(w, s_table);
            case 39: return Ops.AddThenUnsigned(w, x);
            case 40: return Ops.ShlThenLong(w, x);
            case 41: return Ops.NegThenShift(w, x);
            case 42: return Ops.AddThenCall(w, x);
            case 43: return Ops.StoreLocal(w, x);
            case 44: return Ops.ToLong(w, x);
            case 45: return Ops.ToULong(w, x);
            case 46: return Ops.Equal(w, x);
            case 47: return Ops.Less(w, x);
            case 48: return Ops.LessUnsigned(w, x);
            case 49: return Ops.IsZero(w, x);
            case 50: return Ops.IsNegative(w, x);
            case 51: return Ops.IsPositive(w, x);
            case 52: return Ops.LessConst(w, x);
            case 53: return Ops.EqualLong(w, x);
            case 54: return Ops.Div(w, x);
            case 55: return Ops.Rem(w, x);
            case 56: return Ops.DivBy(w, x);
            case 57: return Ops.DivUnsigned(w, x);
            case 58: return Ops.DivConst(w, x);
            case 59: return Ops.RemConst(w, x);
            case 60: return Ops.Or(w, x);
            case 61: return Ops.Xor(w, x);
            case 62: return Ops.Not(w, x);
            case 63: return Ops.And(w, x);
            case 64: return Ops.AndNegative(w, x);
            case 65: return Ops.AndLong(w, x);
            case 66: return Ops.LongShl(w, x);
            case 67: return Ops.LongMul(w, x);
            case 68: return Ops.LongAdd(w, x);
            case 69: return Ops.UnsignedMul(w, x);
            case 70: return Ops.Argument(w, x);
            case 71: return Ops.ArgumentSixth(w, x);
            case 72: return Ops.Return(w);
            case 73: return Ops.Index(w, s_table);
            case 74: return Ops.IndexMasked(w, s_table);
            case 75: return Ops.Select(w, x);
            case 76: return Ops.Max(w, x);
            case 77: return Ops.Abs(w, x);
            case 78: return Ops.Switch(w, x);
            case 79: return Ops.CheckedAdd(w, x);
            case 80: return Ops.CheckedSub(w, x);
            case 81: return Ops.CheckedMul(w, x);
            case 82: return Ops.CheckedUnsignedAdd(w, x);
            case 83: return Ops.CheckedToShort(w, x);
            case 84: return Ops.CheckedToByte(w, x);
            case 85: return Ops.CheckedToUInt(w, x);
            default: return Ops.LoopSum(w, x);
        }
    }

    static void FirstField()
    {
        Words w = new Words();
        for (int vi = 0; vi < s_values.Length; vi++)
        {
            for (int hi = 0; hi < s_high.Length; hi++)
            {
                int f = s_values[vi], high = s_high[hi];
                int b = s_values[(vi * 7 + 3) % s_values.Length], last = s_values[(vi * 5 + 1) % s_values.Length];
                w.A = f;
                w.AHigh = high;
                w.B = b;
                w.BHigh = ~high;
                w.L = -1;
                w.S0 = -1;
                w.S1 = -1;
                w.SHigh = -1;
                w.Last = last;
                for (int xi = 0; xi < s_x.Length; xi++)
                {
                    int x = s_x[xi];
                    for (int op = 0; op <= OpCount; op++)
                    {
                        if (!Defined(op, f, b, x))
                            continue;
                        // A throw costs many steps: try the checked operations with a few operands only.
                        if ((op >= 79) && (op <= 85) && ((vi + xi + hi) % 3 != 0))
                            continue;
                        Check(Name(op), f, high, x, Expected(op, f, b, last, x), Call(op, w, x));
                    }
                }
                Check("the field after the loads", f, high, 0, f, w.A);
                Check("the neighbour after the loads", f, high, 0, high, w.AHigh);
            }
        }
    }

    [MethodImpl(Plain)]
    static void Reset(Words d, int[] ints, Holder h)
    {
        d.A = 0x11111111; d.AHigh = 0x22222222; d.B = 0x33333333; d.BHigh = 0x44444444;
        d.L = 0x5555555555555555; d.S0 = 0x6666; d.S1 = 0x7777; d.SHigh = unchecked((int)0x88888888); d.Last = unchecked((int)0x99999999);
        for (int i = 0; i < ints.Length; i++)
            ints[i] = 0x0A0A0A0A + i;
        h.Before = 0x0B0B0B0B0B0B0B0B; h.P.Low = 0x0C0C0C0C; h.P.High = 0x0D0D0D0D; h.Q.Low = 0x0E0E0E0E; h.Q.High = 0x0F0F0F0F;
        h.After = 0x1010101010101010;
    }

    // The state of everything a store could reach, as one number.
    [MethodImpl(Plain)]
    static long State(Words d, int[] ints, Holder h, long* raw)
    {
        long s = d.A;
        s = s * 31 + d.AHigh; s = s * 31 + d.B; s = s * 31 + d.BHigh; s = s * 31 + d.L; s = s * 31 + d.S0;
        s = s * 31 + d.S1; s = s * 31 + d.SHigh; s = s * 31 + d.Last;
        for (int i = 0; i < ints.Length; i++)
            s = s * 31 + ints[i];
        s = s * 31 + h.Before; s = s * 31 + h.P.Low; s = s * 31 + h.P.High; s = s * 31 + h.Q.Low; s = s * 31 + h.Q.High;
        s = s * 31 + h.After;
        s = s * 31 + raw[0]; s = s * 31 + raw[1]; s = s * 31 + raw[2];
        return s;
    }

    static void Stores()
    {
        Words w = new Words(), d = new Words(), e = new Words();
        int[] ints = new int[6], eints = new int[6];
        Holder h = new Holder(), eh = new Holder();
        long* raw = stackalloc long[3];
        long* eraw = stackalloc long[3];

        for (int vi = 0; vi < s_values.Length; vi++)
        {
            for (int hi = 0; hi < s_high.Length; hi++)
            {
                int f = s_values[vi], high = s_high[hi];
                w.A = f; w.AHigh = high; w.B = ~f; w.BHigh = high; w.Last = f ^ 0x55; w.L = -1;
                for (int op = 0; op < 10; op++)
                {
                    Reset(d, ints, h);
                    Reset(e, eints, eh);
                    raw[0] = raw[1] = raw[2] = 0x1212121212121212;
                    eraw[0] = eraw[1] = eraw[2] = 0x1212121212121212;
                    switch (op)
                    {
                        case 0: e.B = f; Ops.StoreField(d, w); break;
                        case 1: e.Last = f; Ops.StoreLast(d, w); break;
                        case 2: e.S0 = (short)f; Ops.StoreShort(d, w); break;
                        case 3: eints[vi % 6] = f; Ops.StoreElement(ints, vi % 6, w); break;
                        case 4: eints[2] = f; Ops.StoreConstElement(ints, w); break;
                        case 5: *(int*)(eraw + 1) = f; Ops.StorePointer((int*)(raw + 1), w); break;
                        case 6: *(byte*)(eraw + 1) = (byte)f; Ops.StoreBytePointer((byte*)(raw + 1), w); break;
                        case 7: eints[5] = f; Ops.StoreRef(ref ints[5], w); break;
                        case 8: eh.P.Low = f; Ops.StoreStruct(h, w); break;
                        default:
                            eints[0] = f; eints[1] = ~f; eints[2] = f ^ 0x55; eints[3] = f + ~f;
                            Check("StoreMany result", f, high, op, 6, Ops.StoreMany(w, ints));
                            break;
                    }
                    Check("store " + op, f, high, op, State(e, eints, eh, eraw), State(d, ints, h, raw));
                }
            }
        }
    }

    static void OtherPlaces()
    {
        Words w = new Words();
        Holder h = new Holder();
        int[] a = new int[5];
        short[] shorts = new short[21];
        for (int vi = 0; vi < s_values.Length; vi++)
        {
            for (int hi = 0; hi < s_high.Length; hi++)
            {
                int f = s_values[vi], high = s_high[hi], g = s_values[(vi * 3 + 2) % s_values.Length];
                w.A = high; w.AHigh = high; w.B = g; w.BHigh = high; w.L = -1; w.S0 = (short)f; w.S1 = (short)high; w.SHigh = high;
                w.Last = f;
                h.Before = -1; h.P.Low = f; h.P.High = high; h.Q.Low = g; h.Q.High = high; h.After = -1;
                a[0] = f; a[1] = high; a[2] = g; a[3] = high; a[4] = f ^ g;
                object boxed = f;
                for (int xi = 0; xi < s_x.Length; xi++)
                {
                    int x = s_x[xi];
                    Check("LastAdd", f, high, x, Expected(0, f, 0, 0, x), Ops.LastAdd(w, x));
                    Check("LastShl", f, high, x, Expected(5, f, 0, 0, x), Ops.LastShl(w, x));
                    Check("LastLong", f, high, x, f, Ops.LastLong(w, x));
                    Check("LastLess", f, high, x, Expected(47, f, 0, 0, x), Ops.LastLess(w, x));
                    Check("SecondAdd", g, high, x, Expected(0, g, 0, 0, x), Ops.SecondAdd(w, x));
                    Check("SecondMul", g, high, x, Expected(3, g, 0, 0, x), Ops.SecondMul(w, x));
                    Check("SecondLong", g, high, x, g, Ops.SecondLong(w, x));
                    if (Defined(54, g, 0, x))
                        Check("SecondDiv", g, high, x, Expected(54, g, 0, 0, x), Ops.SecondDiv(w, x));
                    Check("ShortAdd", f, high, x, Expected(0, (short)f, 0, 0, x), Ops.ShortAdd(w, x));
                    Check("ShortLong", f, high, x, (short)f, Ops.ShortLong(w, x));

                    Check("StructAdd", f, high, x, Expected(0, f, 0, 0, x), Ops.StructAdd(h, x));
                    Check("StructSub", g, high, x, Expected(1, g, 0, 0, x), Ops.StructSub(h, x));
                    Check("StructLong", f, high, x, f, Ops.StructLong(h, x));
                    Check("StructLess", g, high, x, Expected(47, g, 0, 0, x), Ops.StructLess(h, x));
                    Check("StructHighAdd", high, 0, x, Expected(0, high, 0, 0, x), Ops.StructHighAdd(h, x));

                    Check("Element0Add", f, high, x, Expected(0, f, 0, 0, x), Ops.Element0Add(a, x));
                    Check("Element2Mul", g, high, x, Expected(3, g, 0, 0, x), Ops.Element2Mul(a, x));
                    Check("Element4Shl", f ^ g, 0, x, Expected(5, f ^ g, 0, 0, x), Ops.Element4Shl(a, x));
                    Check("Element1Add", high, 0, x, Expected(0, high, 0, 0, x), Ops.Element1Add(a, x));
                    Check("Element0Long", f, high, x, f, Ops.Element0Long(a, x));
                    Check("Element2Less", g, high, x, Expected(47, g, 0, 0, x), Ops.Element2Less(a, x));
                    if (Defined(54, f ^ g, 0, x))
                        Check("Element4Div", f ^ g, 0, x, Expected(54, f ^ g, 0, 0, x), Ops.Element4Div(a, x));
                    Check("ElementsSum", f, high, x, f + g + (f ^ g) + x, Ops.ElementsSum(a, x));
                    Check("ElementAt 0", f, high, x, Expected(0, f, 0, 0, x), Ops.ElementAt(a, 0, x));
                    Check("ElementAt 4", f ^ g, 0, x, Expected(0, f ^ g, 0, 0, x), Ops.ElementAt(a, 4, x));

                    Check("BoxedAdd", f, 0, x, Expected(0, f, 0, 0, x), Ops.BoxedAdd(boxed, x));
                    Check("BoxedLong", f, 0, x, f, Ops.BoxedLong(boxed, x));
                    Check("BoxedLess", f, 0, x, Expected(47, f, 0, 0, x), Ops.BoxedLess(boxed, x));
                }
            }
        }

        // The characters of a string follow its length; two of them are the upper half of that word.
        string[] strings = { "", "￿", "￿￿", "耀翿￿", "￿￿￿￿￿￿￿￿￿" };
        foreach (string s in strings)
        {
            string t = new string(s.AsSpan());
            int n = s.Length;
            for (int xi = 0; xi < s_x.Length; xi++)
            {
                int x = s_x[xi];
                Check("StringLengthAdd", n, -1, x, Expected(0, n, 0, 0, x), Ops.StringLengthAdd(t, x));
                Check("StringLengthShl", n, -1, x, Expected(5, n, 0, 0, x), Ops.StringLengthShl(t, x));
                Check("StringLengthMul", n, -1, x, Expected(3, n, 0, 0, x), Ops.StringLengthMul(t, x));
                Check("StringLengthLong", n, -1, x, (long)n << 31, Ops.StringLengthLong(t, x));
                Check("ArrayLengthAdd", 21, 0, x, Expected(0, 21, 0, 0, x), Ops.ArrayLengthAdd(shorts, x));
                Check("ArrayLengthSub", 21, 0, x, Expected(2, 21, 0, 0, x), Ops.ArrayLengthSub(shorts, x));
            }
        }
    }

    static int Main()
    {
        for (int i = 0; i < s_table.Length; i++)
            s_table[i] = i * 0x100000007L + 13;

        FirstField();
        EndGroup("the first field of an object, used in every way");

        Stores();
        EndGroup("stores of a field loaded with its neighbour");

        OtherPlaces();
        EndGroup("other fields, struct fields, array elements, a boxed int, lengths");

        Console.WriteLine(s_fails == 0 ? "ALL PASS" : "FAILED " + s_fails);
        return s_fails == 0 ? 0 : 1;
    }
}
