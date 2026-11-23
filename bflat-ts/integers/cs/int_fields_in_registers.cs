// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// 32-bit fields of structs that arrive in registers.
//
// A struct of two ints is passed in one 64-bit register, the second field
// in its upper half, and a struct of four in two registers. The method that
// receives it reads a field straight from that register. The low half is
// the field; the upper half is the other field, not a sign extension. What
// reads the whole register has to extend the field first: a division and
// its checks for zero and for MinValue / -1, a division by a constant made
// of a high multiply, checked arithmetic, a checked narrowing, a conversion
// to long, an index of an array or of a pointer.
//
// The cases take structs of two and four ints, of an int and a uint, and of
// an int with two shorts, with the upper half zero, positive and negative,
// and use the first field of every register in each of those ways. A
// 32-bit target has every field in its own register and passes for the
// same values.
//
// Each group prints one line, and one more for each case that failed; the
// run ends with ALL PASS or a FAIL count, and the guest returns the number
// of failures. A read through a wrong address may stop the guest instead.
using System;
using System.Runtime.CompilerServices;

struct P2 { public int A; public int B; }
struct U2 { public uint A; public uint B; }
struct P4 { public int A; public int B; public int C; public int D; }
struct PS { public int A; public short B; public short C; }

unsafe class Program
{
    const MethodImplOptions NI = MethodImplOptions.NoInlining;
    static int fails;
    static int groupFails;

    static void Eq(string name, long got, long want)
    {
        if (got != want)
        {
            Console.WriteLine("FAIL " + name + ": got 0x" + got.ToString("x") + ", want 0x" + want.ToString("x"));
            groupFails++;
        }
    }

    static void Group(string name, Action body)
    {
        groupFails = 0;
        body();
        Console.WriteLine((groupFails == 0 ? "ok " : "FAILED ") + name);
        fails += groupFails;
    }

    // 0: returned, 1: DivideByZeroException, 2: OverflowException
    static int Kind(Func<long> f, out long value)
    {
        value = 0;
        try { value = f(); return 0; }
        catch (DivideByZeroException) { return 1; }
        catch (OverflowException) { return 2; }
    }

    static void Returns(string name, Func<long> f, long want)
    {
        int k = Kind(f, out long v);
        if (k != 0) { Console.WriteLine("FAIL " + name + ": threw " + (k == 1 ? "DivideByZero" : "Overflow")); groupFails++; }
        else Eq(name, v, want);
    }

    static void Throws(string name, Func<long> f, int kind)
    {
        int k = Kind(f, out long v);
        if (k != kind) { Console.WriteLine("FAIL " + name + ": outcome " + k + ", want " + kind + " (value 0x" + v.ToString("x") + ")"); groupFails++; }
    }

    // Division.
    [MethodImpl(NI)] static int DivC3(P2 s) => s.A / 3;
    [MethodImpl(NI)] static int DivC7(P2 s) => s.A / 7;
    [MethodImpl(NI)] static int DivCNeg(P2 s) => s.A / -5;
    [MethodImpl(NI)] static int RemC10(P2 s) => s.A % 10;
    [MethodImpl(NI)] static uint UDivC3(U2 s) => s.A / 3;
    [MethodImpl(NI)] static uint URemC10(U2 s) => s.A % 10;
    [MethodImpl(NI)] static int DivBy(int x, P2 s) => x / s.A;
    [MethodImpl(NI)] static int RemBy(int x, P2 s) => x % s.A;
    [MethodImpl(NI)] static int DivOf(P2 s, int y) => s.A / y;
    [MethodImpl(NI)] static int RemOf(P2 s, int y) => s.A % y;
    [MethodImpl(NI)] static uint UDivBy(uint x, U2 s) => x / s.A;
    [MethodImpl(NI)] static uint UDivOf(U2 s, uint y) => s.A / y;
    [MethodImpl(NI)] static int DivFields(P4 s) => s.A / s.C;
    [MethodImpl(NI)] static int DivC3Second(P4 s) => s.C / 3;
    [MethodImpl(NI)] static int DivC3Short(PS s) => s.A / 3;

    // Checked arithmetic and checked narrowing.
    [MethodImpl(NI)] static int Add(P2 s, int y) => checked(s.A + y);
    [MethodImpl(NI)] static int Sub(P2 s, int y) => checked(s.A - y);
    [MethodImpl(NI)] static int SubFrom(int x, P2 s) => checked(x - s.A);
    [MethodImpl(NI)] static int Mul(P2 s, int y) => checked(s.A * y);
    [MethodImpl(NI)] static uint UAdd(U2 s, uint y) => checked(s.A + y);
    [MethodImpl(NI)] static uint USub(U2 s, uint y) => checked(s.A - y);
    [MethodImpl(NI)] static uint UMul(U2 s, uint y) => checked(s.A * y);
    [MethodImpl(NI)] static int AddFields(P4 s) => checked(s.A + s.C);
    [MethodImpl(NI)] static short ToShort(P2 s) => checked((short)s.A);
    [MethodImpl(NI)] static sbyte ToSByte(P2 s) => checked((sbyte)s.A);
    [MethodImpl(NI)] static byte ToByte(P2 s) => checked((byte)s.A);
    [MethodImpl(NI)] static ushort ToUShort(P2 s) => checked((ushort)s.A);
    [MethodImpl(NI)] static uint ToUInt(P2 s) => checked((uint)s.A);
    [MethodImpl(NI)] static int ToInt(U2 s) => checked((int)s.A);
    [MethodImpl(NI)] static byte UToByte(U2 s) => checked((byte)s.A);

    // Widening and uses as a 64-bit value.
    [MethodImpl(NI)] static long ToLong(P2 s) => s.A;
    [MethodImpl(NI)] static ulong ToULong(U2 s) => s.A;
    [MethodImpl(NI)] static long ToLongSecond(P4 s) => s.C;
    [MethodImpl(NI)] static long MulLong(P2 s, long y) => s.A * y;
    [MethodImpl(NI)] static long AddLong(P2 s, long y) => s.A + y;
    [MethodImpl(NI)] static long Take(long v) => v;
    [MethodImpl(NI)] static long PassLong(P2 s) => Take(s.A);
    [MethodImpl(NI)] static long BigMul(P2 s, int y) => Math.BigMul(s.A, y);
    [MethodImpl(NI)] static ulong UBigMul(U2 s, uint y) => Math.BigMul(s.A, y);
    [MethodImpl(NI)] static int Shift(int x, P2 s) => x << s.A;
    [MethodImpl(NI)] static long ShiftLong(long x, P2 s) => x >> s.A;
    [MethodImpl(NI)] static int Cmp(P2 s, int y) => s.A < y ? 1 : s.A == y ? 2 : 3;
    [MethodImpl(NI)] static int UCmp(U2 s, uint y) => s.A < y ? 1 : s.A == y ? 2 : 3;

    // Indexing.
    [MethodImpl(NI)] static byte ByteAt(byte[] a, P2 s) => a[s.A];
    [MethodImpl(NI)] static byte ByteAtPlus(byte[] a, P2 s) => a[s.A + 1];
    [MethodImpl(NI)] static long LongAt(long[] a, P2 s) => a[s.A];
    [MethodImpl(NI)] static long LongAtPlus(long[] a, P2 s) => a[s.A + 2];
    [MethodImpl(NI)] static int IntAtSecond(int[] a, P4 s) => a[s.C + 1];
    [MethodImpl(NI)] static byte PtrAt(byte* p, P2 s) => p[s.A];
    [MethodImpl(NI)] static byte PtrAtPlus(byte* p, P2 s) => p[s.A + 1];
    [MethodImpl(NI)] static byte PtrAtMinus(byte* p, P2 s) => p[s.A - 1];
    [MethodImpl(NI)] static long PtrLongAt(long* p, P2 s) => p[s.A + 1];
    [MethodImpl(NI)] static byte PtrAtLong(byte* p, long a) => p[(int)(a + 1)];
    [MethodImpl(NI)] static byte SpanAt(ReadOnlySpan<byte> a, P2 s) => a[s.A + 1];
    [MethodImpl(NI)] static ref byte RefAt(ref byte r, P2 s) => ref Unsafe.Add(ref r, s.A + 1);

    // The upper halves the fields are tried with.
    static readonly int[] Uppers = { 0, 1, 7, -1, int.MinValue, int.MaxValue, 0x55555555 };

    static void Division()
    {
        foreach (int b in Uppers)
        {
            string t = " (other field 0x" + b.ToString("x") + ")";
            foreach (int a in new[] { 0, 1, 9, -9, 100, -100, int.MaxValue, int.MinValue, 0x12345678 })
            {
                var s = new P2 { A = a, B = b };
                Eq("a / 3" + t, DivC3(s), a / 3);
                Eq("a / 7" + t, DivC7(s), a / 7);
                Eq("a / -5" + t, DivCNeg(s), a / -5);
                Eq("a % 10" + t, RemC10(s), a % 10);
                Eq("a / 3, struct with shorts" + t, DivC3Short(new PS { A = a, B = (short)b, C = (short)(b >> 16) }), a / 3);
                Eq("c / 3" + t, DivC3Second(new P4 { A = b, B = b, C = a, D = b }), a / 3);
                var u = new U2 { A = (uint)a, B = (uint)b };
                Eq("ua / 3" + t, UDivC3(u), (uint)a / 3);
                Eq("ua % 10" + t, URemC10(u), (uint)a % 10);
                if (a != 0)
                {
                    Returns("1000 / a" + t, () => DivBy(1000, s), 1000 / a);
                    Returns("1000 % a" + t, () => RemBy(1000, s), 1000 % a);
                    Returns("1000 / ua" + t, () => UDivBy(1000, u), 1000u / (uint)a);
                    Returns("ua / 7" + t, () => UDivOf(u, 7), (uint)a / 7);
                    Returns("a / c" + t, () => DivFields(new P4 { A = 1000, B = b, C = a, D = b }), 1000 / a);
                }
                Returns("a / 7 by a variable" + t, () => DivOf(s, 7), a / 7);
                Returns("a % 7 by a variable" + t, () => RemOf(s, 7), a % 7);
            }
            var zero = new P2 { A = 0, B = b };
            Throws("5 / 0" + t, () => DivBy(5, zero), 1);
            Throws("5 % 0" + t, () => RemBy(5, zero), 1);
            Throws("5u / 0u" + t, () => UDivBy(5, new U2 { A = 0, B = (uint)b }), 1);
            Throws("a / 0 field" + t, () => DivFields(new P4 { A = 5, B = b, C = 0, D = b }), 1);
            var minusOne = new P2 { A = -1, B = b };
            Throws("MinValue / -1" + t, () => DivBy(int.MinValue, minusOne), 2);
            Returns("7 / -1" + t, () => DivBy(7, minusOne), -7);
            Throws("MinValue field / -1" + t, () => DivOf(new P2 { A = int.MinValue, B = b }, -1), 2);
        }
    }

    static void Checked()
    {
        foreach (int b in Uppers)
        {
            string t = " (other field 0x" + b.ToString("x") + ")";
            Returns("1 + 1" + t, () => Add(new P2 { A = 1, B = b }, 1), 2);
            Returns("-5 + 3" + t, () => Add(new P2 { A = -5, B = b }, 3), -2);
            Throws("MaxValue + 1" + t, () => Add(new P2 { A = int.MaxValue, B = b }, 1), 2);
            Throws("MinValue + -1" + t, () => Add(new P2 { A = int.MinValue, B = b }, -1), 2);
            Returns("1 - 1" + t, () => Sub(new P2 { A = 1, B = b }, 1), 0);
            Throws("MinValue - 1" + t, () => Sub(new P2 { A = int.MinValue, B = b }, 1), 2);
            Returns("10 - 3 from a field" + t, () => SubFrom(10, new P2 { A = 3, B = b }), 7);
            Throws("0 - MinValue" + t, () => SubFrom(0, new P2 { A = int.MinValue, B = b }), 2);
            Returns("3 * 5" + t, () => Mul(new P2 { A = 3, B = b }, 5), 15);
            Returns("-3 * 5" + t, () => Mul(new P2 { A = -3, B = b }, 5), -15);
            Throws("65536 * 65536" + t, () => Mul(new P2 { A = 65536, B = b }, 65536), 2);
            Returns("1u + 1u" + t, () => UAdd(new U2 { A = 1, B = (uint)b }, 1), 2);
            Throws("MaxValue u + 1" + t, () => UAdd(new U2 { A = uint.MaxValue, B = (uint)b }, 1), 2);
            Returns("5u - 3u" + t, () => USub(new U2 { A = 5, B = (uint)b }, 3), 2);
            Throws("3u - 5u" + t, () => USub(new U2 { A = 3, B = (uint)b }, 5), 2);
            Returns("3u * 5u" + t, () => UMul(new U2 { A = 3, B = (uint)b }, 5), 15);
            Throws("65536u * 65536u" + t, () => UMul(new U2 { A = 65536, B = (uint)b }, 65536), 2);
            Returns("a + c" + t, () => AddFields(new P4 { A = 4, B = b, C = 5, D = b }), 9);
            Throws("a + c overflow" + t, () => AddFields(new P4 { A = int.MaxValue, B = b, C = 1, D = b }), 2);

            Returns("(short)300" + t, () => ToShort(new P2 { A = 300, B = b }), 300);
            Returns("(short)-300" + t, () => ToShort(new P2 { A = -300, B = b }), -300);
            Throws("(short)40000" + t, () => ToShort(new P2 { A = 40000, B = b }), 2);
            Returns("(sbyte)-128" + t, () => ToSByte(new P2 { A = -128, B = b }), -128);
            Throws("(sbyte)128" + t, () => ToSByte(new P2 { A = 128, B = b }), 2);
            Returns("(byte)255" + t, () => ToByte(new P2 { A = 255, B = b }), 255);
            Throws("(byte)-1" + t, () => ToByte(new P2 { A = -1, B = b }), 2);
            Returns("(ushort)65535" + t, () => ToUShort(new P2 { A = 65535, B = b }), 65535);
            Throws("(ushort)65536" + t, () => ToUShort(new P2 { A = 65536, B = b }), 2);
            Returns("(uint)5" + t, () => ToUInt(new P2 { A = 5, B = b }), 5);
            Throws("(uint)-5" + t, () => ToUInt(new P2 { A = -5, B = b }), 2);
            Returns("(int)5u" + t, () => ToInt(new U2 { A = 5, B = (uint)b }), 5);
            Throws("(int)0x80000000u" + t, () => ToInt(new U2 { A = 0x80000000, B = (uint)b }), 2);
            Returns("(byte)200u" + t, () => UToByte(new U2 { A = 200, B = (uint)b }), 200);
            Throws("(byte)256u" + t, () => UToByte(new U2 { A = 256, B = (uint)b }), 2);
        }
    }

    static void Widening()
    {
        foreach (int b in Uppers)
        {
            string t = " (other field 0x" + b.ToString("x") + ")";
            foreach (int a in new[] { 0, 1, -1, 3, int.MaxValue, int.MinValue, 0x12345678 })
            {
                var s = new P2 { A = a, B = b };
                var u = new U2 { A = (uint)a, B = (uint)b };
                Eq("(long)a" + t, ToLong(s), a);
                Eq("(ulong)ua" + t, (long)ToULong(u), (uint)a);
                Eq("(long)c" + t, ToLongSecond(new P4 { A = b, B = b, C = a, D = b }), a);
                Eq("a * long" + t, MulLong(s, 1000003), a * 1000003L);
                Eq("a + long" + t, AddLong(s, 0x1_0000_0000L), a + 0x1_0000_0000L);
                Eq("a as a long argument" + t, PassLong(s), a);
                Eq("BigMul(a, 77)" + t, BigMul(s, 77), (long)a * 77);
                Eq("BigMul(ua, 77)" + t, (long)UBigMul(u, 77), (long)((ulong)(uint)a * 77));
                Eq("5 << a" + t, Shift(5, s), 5 << a);
                Eq("long >> a" + t, ShiftLong(long.MinValue, s), long.MinValue >> a);
                Eq("a ? 3" + t, Cmp(s, 3), a < 3 ? 1 : a == 3 ? 2 : 3);
                Eq("ua ? 3" + t, UCmp(u, 3), (uint)a < 3 ? 1 : (uint)a == 3 ? 2 : 3);
            }
        }
    }

    static void Indexing()
    {
        var bytes = new byte[16];
        var longs = new long[16];
        var ints = new int[16];
        byte* buf = stackalloc byte[32];
        long* lbuf = stackalloc long[16];
        for (int i = 0; i < 16; i++) { bytes[i] = (byte)(0x10 + i); longs[i] = 0x1000 + i; ints[i] = 0x100 + i; lbuf[i] = 0x2000 + i; }
        for (int i = 0; i < 32; i++) buf[i] = (byte)(0x40 + i);

        foreach (int b in Uppers)
        {
            string t = " (other field 0x" + b.ToString("x") + ")";
            var s = new P2 { A = 3, B = b };
            Eq("bytes[a]" + t, ByteAt(bytes, s), 0x13);
            Eq("bytes[a + 1]" + t, ByteAtPlus(bytes, s), 0x14);
            Eq("longs[a]" + t, LongAt(longs, s), 0x1003);
            Eq("longs[a + 2]" + t, LongAtPlus(longs, s), 0x1005);
            Eq("ints[c + 1]" + t, IntAtSecond(ints, new P4 { A = b, B = b, C = 3, D = b }), 0x104);
            Eq("span[a + 1]" + t, SpanAt(bytes, s), 0x14);
            Eq("ref + a + 1" + t, RefAt(ref bytes[0], s), 0x14);
            Eq("p[a]" + t, PtrAt(buf + 8, s), 0x4b);
            Eq("p[a + 1]" + t, PtrAtPlus(buf + 8, s), 0x4c);
            Eq("p[a - 1]" + t, PtrAtMinus(buf + 8, s), 0x4a);
            Eq("p[-3]" + t, PtrAt(buf + 8, new P2 { A = -3, B = b }), 0x45);
            Eq("p[-3 + 1]" + t, PtrAtPlus(buf + 8, new P2 { A = -3, B = b }), 0x46);
            Eq("lp[a + 1]" + t, PtrLongAt(lbuf, s), 0x2004);
            Eq("p[(int)(long + 1)]" + t, PtrAtLong(buf + 8, ((long)b << 32) | 3), 0x4c);
        }
    }

    static int Main()
    {
        Group("division", Division);
        Group("checked arithmetic and narrowing", Checked);
        Group("widening", Widening);
        Group("indexing", Indexing);
        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAIL count " + fails);
        return fails;
    }
}
