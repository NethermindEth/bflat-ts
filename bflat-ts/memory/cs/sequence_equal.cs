// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// SequenceEqual of a constant length.
//
// A comparison of two spans whose length the JIT knows is a candidate for an
// expansion in line; on ZisK (JitRiscV64DmaCompare) it becomes the memcmp
// precompile, an instruction pair that names the two addresses, the length
// as an immediate and the register that receives the difference. What can go
// wrong: a difference in one byte that is missed (the first, the last, one
// in a tail of one to seven bytes), bytes before the first or after the last
// that are compared too, the two addresses or the result taken from the
// wrong registers, a result that is not turned into true and false the right
// way round, a length that does not fit the immediate, and values in other
// registers that do not survive the comparison.
//
// Every length from 1 to 41 and lengths on both sides of 48, 56, 64, 72,
// 128, 256, 512, 1024, 2048 (the largest immediate is 2047) and 4096 are
// compared with the two spans at different distances from an 8-byte
// boundary: equal contents, one byte changed at every position in turn (by
// one bit, by the top bit and by all bits), and a byte changed right before
// and right after the spans. The result is used as a value, negated, as a
// branch condition, with other values live around it, and three times in one
// method. Spans of chars and of longs, a span compared with itself, two
// lengths that differ and a length that is not constant are compared too.
//
// The expected result comes from a loop that compares one byte at a time and
// is compiled without optimization. Each group prints one line; the run ends
// with ALL PASS or a FAIL count.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

unsafe struct Z1 { public fixed byte B[1]; }
unsafe struct Z2 { public fixed byte B[2]; }
unsafe struct Z3 { public fixed byte B[3]; }
unsafe struct Z4 { public fixed byte B[4]; }
unsafe struct Z5 { public fixed byte B[5]; }
unsafe struct Z6 { public fixed byte B[6]; }
unsafe struct Z7 { public fixed byte B[7]; }
unsafe struct Z8 { public fixed byte B[8]; }
unsafe struct Z9 { public fixed byte B[9]; }
unsafe struct Z10 { public fixed byte B[10]; }
unsafe struct Z11 { public fixed byte B[11]; }
unsafe struct Z12 { public fixed byte B[12]; }
unsafe struct Z13 { public fixed byte B[13]; }
unsafe struct Z14 { public fixed byte B[14]; }
unsafe struct Z15 { public fixed byte B[15]; }
unsafe struct Z16 { public fixed byte B[16]; }
unsafe struct Z17 { public fixed byte B[17]; }
unsafe struct Z18 { public fixed byte B[18]; }
unsafe struct Z19 { public fixed byte B[19]; }
unsafe struct Z20 { public fixed byte B[20]; }
unsafe struct Z21 { public fixed byte B[21]; }
unsafe struct Z22 { public fixed byte B[22]; }
unsafe struct Z23 { public fixed byte B[23]; }
unsafe struct Z24 { public fixed byte B[24]; }
unsafe struct Z25 { public fixed byte B[25]; }
unsafe struct Z26 { public fixed byte B[26]; }
unsafe struct Z27 { public fixed byte B[27]; }
unsafe struct Z28 { public fixed byte B[28]; }
unsafe struct Z29 { public fixed byte B[29]; }
unsafe struct Z30 { public fixed byte B[30]; }
unsafe struct Z31 { public fixed byte B[31]; }
unsafe struct Z32 { public fixed byte B[32]; }
unsafe struct Z33 { public fixed byte B[33]; }
unsafe struct Z34 { public fixed byte B[34]; }
unsafe struct Z35 { public fixed byte B[35]; }
unsafe struct Z36 { public fixed byte B[36]; }
unsafe struct Z37 { public fixed byte B[37]; }
unsafe struct Z38 { public fixed byte B[38]; }
unsafe struct Z39 { public fixed byte B[39]; }
unsafe struct Z40 { public fixed byte B[40]; }
unsafe struct Z41 { public fixed byte B[41]; }
unsafe struct Z47 { public fixed byte B[47]; }
unsafe struct Z48 { public fixed byte B[48]; }
unsafe struct Z49 { public fixed byte B[49]; }
unsafe struct Z55 { public fixed byte B[55]; }
unsafe struct Z56 { public fixed byte B[56]; }
unsafe struct Z57 { public fixed byte B[57]; }
unsafe struct Z63 { public fixed byte B[63]; }
unsafe struct Z64 { public fixed byte B[64]; }
unsafe struct Z65 { public fixed byte B[65]; }
unsafe struct Z71 { public fixed byte B[71]; }
unsafe struct Z72 { public fixed byte B[72]; }
unsafe struct Z96 { public fixed byte B[96]; }
unsafe struct Z127 { public fixed byte B[127]; }
unsafe struct Z128 { public fixed byte B[128]; }
unsafe struct Z129 { public fixed byte B[129]; }
unsafe struct Z255 { public fixed byte B[255]; }
unsafe struct Z256 { public fixed byte B[256]; }
unsafe struct Z257 { public fixed byte B[257]; }
unsafe struct Z511 { public fixed byte B[511]; }
unsafe struct Z512 { public fixed byte B[512]; }
unsafe struct Z1023 { public fixed byte B[1023]; }
unsafe struct Z1024 { public fixed byte B[1024]; }
unsafe struct Z2046 { public fixed byte B[2046]; }
unsafe struct Z2047 { public fixed byte B[2047]; }
unsafe struct Z2048 { public fixed byte B[2048]; }
unsafe struct Z2049 { public fixed byte B[2049]; }
unsafe struct Z4096 { public fixed byte B[4096]; }

// Byte loops the JIT must not turn into the operation under test.
static unsafe class Ref
{
    const MethodImplOptions Plain = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;

    [MethodImpl(Plain)]
    public static void Fill(byte* p, int n, int seed)
    {
        for (int i = 0; i < n; i++)
            p[i] = (byte)(seed * 29 + i * 7 + (i >> 4) + 1);
    }

    [MethodImpl(Plain)]
    public static void Copy(byte* d, byte* s, int n)
    {
        for (int i = 0; i < n; i++)
            d[i] = s[i];
    }

    [MethodImpl(Plain)]
    public static bool Same(byte* a, byte* b, int n)
    {
        for (int i = 0; i < n; i++)
        {
            if (a[i] != b[i])
                return false;
        }
        return true;
    }
}

// The comparisons under test, one instantiation per length.
static unsafe class Cmp<T> where T : unmanaged
{
    static ReadOnlySpan<byte> Span(byte* p) => new ReadOnlySpan<byte>(p, sizeof(T));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Equal(byte* a, byte* b) => Span(a).SequenceEqual(Span(b));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool NotEqual(byte* a, byte* b) => !Span(a).SequenceEqual(Span(b));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Branch(byte* a, byte* b, long x, long y)
    {
        if (Span(a).SequenceEqual(Span(b)))
            return x * 3 + 1;
        return y * 5 + 2;
    }

    // Values computed before the comparison and used after it.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Live(byte* a, byte* b, long p, long q, long r, long s)
    {
        long u = p * 3 + q, v = q * 5 + r, w = r * 7 + s, x = s * 11 + p;
        bool e = Span(a).SequenceEqual(Span(b));
        long y = u ^ v, z = w ^ x;
        return (e ? 1 : 1000) + u + v * 13 + w * 17 + x * 19 + y * 23 + z * 29 + p + q + r + s;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Three(byte* a, byte* b, byte* c) =>
        (Span(a).SequenceEqual(Span(b)) ? 1 : 0) + (Span(b).SequenceEqual(Span(c)) ? 2 : 0) +
        (Span(c).SequenceEqual(Span(a)) ? 4 : 0);

    // The addresses are formed at the comparison: a base and an offset, an index.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Offsets(byte* a, byte* b, long i) => Span(a + 8).SequenceEqual(Span(b + i));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Arrays(byte[] a, byte[] b) =>
        a.AsSpan(3, sizeof(T)).SequenceEqual(b.AsSpan(5, sizeof(T)));
}

static unsafe class Program
{
    const int Room = 4400;

    static byte* s_a;
    static byte* s_b;
    static byte* s_c;
    static int s_fails;
    static int s_groupFails;

    static void EndGroup(string name)
    {
        if (s_groupFails == 0)
            Console.WriteLine("ok   " + name);
        else
            Console.WriteLine("FAIL " + name + " cases=" + s_groupFails);
        s_fails += s_groupFails;
        s_groupFails = 0;
    }

    static void Check(string name, int n, int am, int bm, int at, long expected, long got)
    {
        if (expected == got)
            return;
        s_groupFails++;
        if (s_groupFails <= 6)
        {
            Console.WriteLine("FAIL " + name + " length " + n + " a+" + am + " b+" + bm + " changed byte " + at +
                              " expected " + expected + " got " + got);
        }
    }

    static long LiveExpected(bool e, long p, long q, long r, long s)
    {
        long u = p * 3 + q, v = q * 5 + r, w = r * 7 + s, x = s * 11 + p;
        return (e ? 1 : 1000) + u + v * 13 + w * 17 + x * 19 + (u ^ v) * 23 + (w ^ x) * 29 + p + q + r + s;
    }

    static void All<T>(int n, byte* a, byte* b, int am, int bm, int at) where T : unmanaged
    {
        bool same = Ref.Same(a, b, n);
        Check("Equal", n, am, bm, at, same ? 1 : 0, Cmp<T>.Equal(a, b) ? 1 : 0);
        Check("Equal swapped", n, am, bm, at, same ? 1 : 0, Cmp<T>.Equal(b, a) ? 1 : 0);
        Check("NotEqual", n, am, bm, at, same ? 0 : 1, Cmp<T>.NotEqual(a, b) ? 1 : 0);
        Check("Branch", n, am, bm, at, same ? 7 * 3 + 1 : 9 * 5 + 2, Cmp<T>.Branch(a, b, 7, 9));
        Check("Live", n, am, bm, at, LiveExpected(same, n, at, am + 100, bm + 1000),
              Cmp<T>.Live(a, b, n, at, am + 100, bm + 1000));
    }

    static void Run<T>() where T : unmanaged
    {
        int n = sizeof(T);
        for (int k = 0; k < 12; k++)
        {
            int am = (k < 8) ? k : (k - 8) * 2 + 1, bm = (k < 8) ? (k * 3 + 1) & 7 : am;
            byte* a = s_a + am;
            byte* b = s_b + bm;
            Ref.Fill(s_a - 16, n + 48, n);
            Ref.Fill(s_b - 16, n + 48, n + 77);
            Ref.Copy(b, a, n);
            All<T>(n, a, b, am, bm, -1);
            Check("Equal same span", n, am, am, -1, 1, Cmp<T>.Equal(a, a) ? 1 : 0);

            // A byte right before and right after the spans differs: still equal.
            b[-1] = (byte)(a[-1] + 1);
            b[n] = (byte)(a[n] + 1);
            All<T>(n, a, b, am, bm, -2);

            int step = (n <= 72) ? 1 : (k == 0) ? 1 : 1 + n / 29;
            for (int i = 0; i < n; i += (i < 9 || i >= n - 10) ? 1 : step)
            {
                byte keep = b[i];
                b[i] = (byte)(keep ^ ((i % 3 == 0) ? 0x01 : (i % 3 == 1) ? 0x80 : 0xFF));
                if ((k < 2) || (n <= 72))
                    All<T>(n, a, b, am, bm, i);
                else
                    Check("Equal", n, am, bm, i, 0, Cmp<T>.Equal(a, b) ? 1 : 0);
                b[i] = keep;
            }
            Check("Equal restored", n, am, bm, -3, 1, Cmp<T>.Equal(a, b) ? 1 : 0);

            // Three spans: a == b, c differs from both in its last byte.
            byte* c = s_c + ((am + 3) & 7);
            Ref.Copy(c, a, n);
            Check("Three equal", n, am, bm, -1, 7, Cmp<T>.Three(a, b, c));
            c[n - 1] ^= 0x10;
            Check("Three", n, am, bm, n - 1, 1, Cmp<T>.Three(a, b, c));
            b[0] ^= 0x10;
            Check("Three all differ", n, am, bm, 0, (n == 1) ? 2 : 0, Cmp<T>.Three(a, b, c));
            b[0] ^= 0x10;

            if (n + 24 < Room - 64)
            {
                Ref.Copy(b + 5, a + 8, n);
                Check("Offsets", n, am, bm, -1, 1, Cmp<T>.Offsets(a, b, 5) ? 1 : 0);
                b[5 + n / 2] ^= 0x04;
                Check("Offsets", n, am, bm, n / 2, 0, Cmp<T>.Offsets(a, b, 5) ? 1 : 0);
            }
        }

        byte[] x = new byte[n + 16], y = new byte[n + 16];
        for (int i = 0; i < n; i++)
        {
            x[3 + i] = (byte)(i * 11 + n);
            y[5 + i] = (byte)(i * 11 + n);
        }
        x[2] = 1; y[4] = 2; x[3 + n] = 3; y[5 + n] = 4;
        Check("Arrays", n, 3, 5, -1, 1, Cmp<T>.Arrays(x, y) ? 1 : 0);
        y[5 + n - 1] ^= 0x40;
        Check("Arrays", n, 3, 5, n - 1, 0, Cmp<T>.Arrays(x, y) ? 1 : 0);
        y[5 + n - 1] ^= 0x40;
        y[5] ^= 0x02;
        Check("Arrays", n, 3, 5, 0, 0, Cmp<T>.Arrays(x, y) ? 1 : 0);
    }

    // ---- other element types, lengths that differ, a length that is not constant ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Chars12(char* a, char* b) => new ReadOnlySpan<char>(a, 12).SequenceEqual(new ReadOnlySpan<char>(b, 12));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Longs5(long* a, long* b) => new ReadOnlySpan<long>(a, 5).SequenceEqual(new ReadOnlySpan<long>(b, 5));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Ints7(int[] a, int[] b) => a.AsSpan(1, 7).SequenceEqual(b.AsSpan(2, 7));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Differ(byte* a, byte* b) => new ReadOnlySpan<byte>(a, 24).SequenceEqual(new ReadOnlySpan<byte>(b, 25));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Variable(byte* a, byte* b, int n) => new ReadOnlySpan<byte>(a, n).SequenceEqual(new ReadOnlySpan<byte>(b, n));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Literal(string s) => s == "a string of thirty-one characters";

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool LiteralSpan(ReadOnlySpan<char> s) => s.SequenceEqual("0123456789abcdefghijklmnopqrstuvwxyz");

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Prefix(ReadOnlySpan<byte> s) => s.StartsWith("content-length: "u8);

    static void Others()
    {
        byte* a = s_a;
        byte* b = s_b;
        Ref.Fill(a, 160, 5);
        Ref.Copy(b, a, 160);
        for (int m = 0; m < 4; m++)
        {
            char* ca = (char*)(a + 2 * m);
            char* cb = (char*)(b + 2 * ((m + 1) & 3));
            Ref.Copy((byte*)cb, (byte*)ca, 24);
            Check("Chars12", 24, m, m, -1, 1, Chars12(ca, cb) ? 1 : 0);
            for (int i = 0; i < 24; i++)
            {
                ((byte*)cb)[i] ^= 0x20;
                Check("Chars12", 24, m, m, i, 0, Chars12(ca, cb) ? 1 : 0);
                ((byte*)cb)[i] ^= 0x20;
            }
            ((byte*)cb)[24] ^= 0x20;
            Check("Chars12 after", 24, m, m, 24, 1, Chars12(ca, cb) ? 1 : 0);
            Ref.Copy(b, a, 160);
        }

        long* la = (long*)(a + 8);
        long* lb = (long*)(b + 16);
        Ref.Copy((byte*)lb, (byte*)la, 40);
        Check("Longs5", 40, 0, 0, -1, 1, Longs5(la, lb) ? 1 : 0);
        for (int i = 0; i < 40; i++)
        {
            ((byte*)lb)[i] ^= 0x80;
            Check("Longs5", 40, 0, 0, i, 0, Longs5(la, lb) ? 1 : 0);
            ((byte*)lb)[i] ^= 0x80;
        }

        int[] ia = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, ib = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        Check("Ints7", 28, 0, 0, -1, 1, Ints7(ia, ib) ? 1 : 0);
        for (int i = 0; i < 10; i++)
        {
            ib[i] ^= 0x01000000;
            Check("Ints7", 28, 0, 0, i, (i >= 2 && i < 9) ? 0 : 1, Ints7(ia, ib) ? 1 : 0);
            ib[i] ^= 0x01000000;
        }

        Ref.Copy(b, a, 160);
        Check("Differ", 24, 0, 0, -1, 0, Differ(a, b) ? 1 : 0);

        for (int n = 0; n <= 100; n++)
        {
            Check("Variable", n, 1, 3, -1, 1, Variable(a + 1, b + 1, n) ? 1 : 0);
            for (int i = 0; i < n; i += (n < 40) ? 1 : 7)
            {
                b[1 + i] ^= 0x08;
                Check("Variable", n, 1, 3, i, 0, Variable(a + 1, b + 1, n) ? 1 : 0);
                b[1 + i] ^= 0x08;
            }
        }

        string lit = new string("a string of thirty-one characters".AsSpan());
        Check("Literal", lit.Length, 0, 0, -1, 1, Literal(lit) ? 1 : 0);
        for (int i = 0; i < lit.Length; i++)
        {
            char[] chars = lit.ToCharArray();
            chars[i] = (char)(chars[i] ^ ((i % 2 == 0) ? 0x0001 : 0x0100));
            Check("Literal", lit.Length, 0, 0, i, 0, Literal(new string(chars)) ? 1 : 0);
        }
        Check("Literal shorter", lit.Length, 0, 0, -1, 0, Literal(lit.Substring(1)) ? 1 : 0);

        char[] alnum = "0123456789abcdefghijklmnopqrstuvwxyz!".ToCharArray();
        Check("LiteralSpan", 36, 0, 0, -1, 1, LiteralSpan(alnum.AsSpan(0, 36)) ? 1 : 0);
        Check("LiteralSpan longer", 37, 0, 0, -1, 0, LiteralSpan(alnum) ? 1 : 0);
        for (int i = 0; i < 36; i++)
        {
            alnum[i] ^= (char)0x4000;
            Check("LiteralSpan", 36, 0, 0, i, 0, LiteralSpan(alnum.AsSpan(0, 36)) ? 1 : 0);
            alnum[i] ^= (char)0x4000;
        }

        byte[] header = new byte[24];
        "content-length: 1234"u8.CopyTo(header);
        Check("Prefix", 16, 0, 0, -1, 1, Prefix(header) ? 1 : 0);
        Check("Prefix short", 15, 0, 0, -1, 0, Prefix(header.AsSpan(0, 15)) ? 1 : 0);
        for (int i = 0; i < 20; i++)
        {
            header[i] ^= 0x01;
            Check("Prefix", 16, 0, 0, i, (i < 16) ? 0 : 1, Prefix(header) ? 1 : 0);
            header[i] ^= 0x01;
        }
    }

    static int Main()
    {
        byte* mem = stackalloc byte[3 * Room];
        s_a = mem + 32;
        s_b = mem + Room + 32;
        s_c = mem + 2 * Room + 32;

        Run<Z1>(); Run<Z2>(); Run<Z3>(); Run<Z4>(); Run<Z5>(); Run<Z6>(); Run<Z7>(); Run<Z8>();
        Run<Z9>(); Run<Z10>(); Run<Z11>(); Run<Z12>(); Run<Z13>(); Run<Z14>(); Run<Z15>();
        Run<Z16>(); Run<Z17>(); Run<Z18>(); Run<Z19>(); Run<Z20>(); Run<Z21>(); Run<Z22>();
        Run<Z23>(); Run<Z24>(); Run<Z25>(); Run<Z26>(); Run<Z27>(); Run<Z28>(); Run<Z29>();
        Run<Z30>(); Run<Z31>(); Run<Z32>(); Run<Z33>(); Run<Z34>(); Run<Z35>(); Run<Z36>();
        Run<Z37>(); Run<Z38>(); Run<Z39>(); Run<Z40>(); Run<Z41>(); Run<Z47>(); Run<Z48>();
        Run<Z49>(); Run<Z55>(); Run<Z56>(); Run<Z57>(); Run<Z63>(); Run<Z64>(); Run<Z65>();
        Run<Z71>(); Run<Z72>(); Run<Z96>(); Run<Z127>(); Run<Z128>(); Run<Z129>(); Run<Z255>();
        Run<Z256>(); Run<Z257>(); Run<Z511>(); Run<Z512>(); Run<Z1023>(); Run<Z1024>();
        Run<Z2046>(); Run<Z2047>(); Run<Z2048>(); Run<Z2049>(); Run<Z4096>();
        EndGroup("every length, distance from an 8-byte boundary and changed byte");

        Others();
        EndGroup("chars, longs, ints, lengths that differ, a variable length, literals");

        Console.WriteLine(s_fails == 0 ? "ALL PASS" : "FAILED " + s_fails);
        return s_fails == 0 ? 0 : 1;
    }
}
