// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Math.BigMul: the 128-bit product of two 64-bit numbers.
//
// The compiler expands Math.BigMul(ulong, ulong, out ulong) into a mul
// whose result is stored through the out parameter and a mulhu that is the
// value returned, and the signed overload into mul and mulh. The factors go
// to temporaries first. What can go wrong: the high half computed after the
// low half was stored over a factor (BigMul(a, b, out a)), the signed and
// the unsigned high half taken for the same value when both are computed
// from the same factors, the order of the factors, the address and the
// store when they have side effects, a store that is lost when the result
// is not used, and a wrong high half for constant factors.
//
// The expected products are computed by schoolbook multiplication of 32-bit
// halves, compiled without optimization, with the sign corrections of the
// signed product applied by hand.
//
// A failed check prints one line; the run ends with ALL PASS or a FAIL
// count, and the guest returns 1 when anything failed.
using System;
using System.Runtime.CompilerServices;

class Cell { public ulong Low; public ulong Other; public long SLow; }

struct Wide { public ulong A; public ulong B; }

unsafe class Program
{
    const MethodImplOptions NI = MethodImplOptions.NoInlining;
    const MethodImplOptions NoOpt = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;

    static int fails;
    static int checks;
    static int shown;
    static int order;
    static long trace;
    static ulong staticLow;

    // ---- reference ----

    [MethodImpl(NoOpt)]
    static ulong RefHigh(ulong a, ulong b, out ulong low)
    {
        ulong al = a & 0xFFFFFFFFUL, ah = a >> 32, bl = b & 0xFFFFFFFFUL, bh = b >> 32;
        ulong ll = al * bl, lh = al * bh, hl = ah * bl, hh = ah * bh;
        ulong mid = (ll >> 32) + (lh & 0xFFFFFFFFUL) + (hl & 0xFFFFFFFFUL);
        low = (ll & 0xFFFFFFFFUL) | (mid << 32);
        return hh + (lh >> 32) + (hl >> 32) + (mid >> 32);
    }

    [MethodImpl(NoOpt)]
    static long RefHighSigned(long a, long b, out long low)
    {
        ulong ulow;
        ulong high = RefHigh((ulong)a, (ulong)b, out ulow);
        if (a < 0) high -= (ulong)b;
        if (b < 0) high -= (ulong)a;
        low = (long)ulow;
        return (long)high;
    }

    // ---- the calls ----

    [MethodImpl(NI)] static ulong Plain(ulong a, ulong b, out ulong low) => Math.BigMul(a, b, out low);
    [MethodImpl(NI)] static long PlainSigned(long a, long b, out long low) => Math.BigMul(a, b, out low);

    [MethodImpl(NI)] static ulong OverFirst(ulong a, ulong b, out ulong low) { ulong h = Math.BigMul(a, b, out a); low = a; return h; }
    [MethodImpl(NI)] static ulong OverSecond(ulong a, ulong b, out ulong low) { ulong h = Math.BigMul(a, b, out b); low = b; return h; }
    [MethodImpl(NI)] static long OverFirstSigned(long a, long b, out long low) { long h = Math.BigMul(a, b, out a); low = a; return h; }
    [MethodImpl(NI)] static long OverSecondSigned(long a, long b, out long low) { long h = Math.BigMul(a, b, out b); low = b; return h; }
    [MethodImpl(NI)] static ulong Square(ulong a, out ulong low) { ulong h = Math.BigMul(a, a, out a); low = a; return h; }
    [MethodImpl(NI)] static ulong OverRef(ref ulong a, ulong b) => Math.BigMul(a, b, out a);
    [MethodImpl(NI)] static ulong OverRefBoth(ref ulong a, ref ulong b) => Math.BigMul(a, b, out b);
    [MethodImpl(NI)] static ulong OverPointer(ulong* p) => Math.BigMul(p[0], p[1], out p[0]);
    [MethodImpl(NI)] static ulong OverPointerSecond(ulong* p) => Math.BigMul(p[0], p[1], out p[1]);
    [MethodImpl(NI)] static ulong OverField(Cell c, ulong b) => Math.BigMul(c.Low, b, out c.Low);
    [MethodImpl(NI)] static ulong OverFieldBoth(Cell c) => Math.BigMul(c.Low, c.Other, out c.Other);
    [MethodImpl(NI)] static ulong OverElement(ulong[] v) => Math.BigMul(v[0], v[1], out v[1]);
    [MethodImpl(NI)] static ulong OverStatic(ulong b) => Math.BigMul(staticLow, b, out staticLow);
    [MethodImpl(NI)] static ulong OverStruct(ref Wide w) => Math.BigMul(w.A, w.B, out w.A);
    [MethodImpl(NI)] static ulong OverHigh(ulong a, ulong b, out ulong low) { ulong h = a; h = Math.BigMul(h, b, out low); return h; }

    [MethodImpl(NI)] static ulong HighOnly(ulong a, ulong b) => Math.BigMul(a, b, out _);
    [MethodImpl(NI)] static long HighOnlySigned(long a, long b) => Math.BigMul(a, b, out _);
    [MethodImpl(NI)] static ulong LowOnly(ulong a, ulong b) { Math.BigMul(a, b, out ulong low); return low; }
    [MethodImpl(NI)] static long LowOnlySigned(long a, long b) { Math.BigMul(a, b, out long low); return low; }
    [MethodImpl(NI)] static void StoreOnly(ulong a, ulong b, Cell c) { Math.BigMul(a, b, out c.Low); }

    // Both overloads on the same bits: the high halves differ.
    [MethodImpl(NI)]
    static ulong BothKinds(ulong a, ulong b, out long signedHigh, out ulong low, out long signedLow)
    {
        ulong high = Math.BigMul(a, b, out low);
        signedHigh = Math.BigMul((long)a, (long)b, out signedLow);
        return high;
    }

    [MethodImpl(NI)]
    static ulong BothKindsReversed(ulong a, ulong b, out long signedHigh)
    {
        signedHigh = Math.BigMul((long)b, (long)a, out long sl);
        ulong high = Math.BigMul(a, b, out ulong low);
        return high + low + (ulong)sl;
    }

    // The same product twice, and with the factors swapped.
    [MethodImpl(NI)]
    static ulong Twice(ulong a, ulong b, out ulong low)
    {
        ulong h1 = Math.BigMul(a, b, out ulong l1);
        ulong h2 = Math.BigMul(b, a, out ulong l2);
        ulong h3 = Math.BigMul(a, b, out ulong l3);
        low = l1 ^ l2 ^ l3;
        return h1 ^ (h2 * 3) ^ (h3 * 5);
    }

    // The product and its parts in other arithmetic.
    [MethodImpl(NI)]
    static ulong Mixed(ulong a, ulong b, ulong c)
    {
        ulong h = Math.BigMul(a, b, out ulong l);
        ulong plain = a * b;
        return h * c + l + (plain ^ l) + Math.BigMul(h, l, out ulong l2) + l2;
    }

    [MethodImpl(NoOpt)]
    static ulong RMixed(ulong a, ulong b, ulong c)
    {
        ulong h = RefHigh(a, b, out ulong l);
        ulong h2 = RefHigh(h, l, out ulong l2);
        return h * c + l + h2 + l2;
    }

    // UInt128 and Int128 multiply through the same method.
    [MethodImpl(NI)] static UInt128 Full(ulong a, ulong b) => Math.BigMul(a, b);
    [MethodImpl(NI)] static Int128 FullSigned(long a, long b) => Math.BigMul(a, b);
    [MethodImpl(NI)] static UInt128 Mul128(UInt128 a, UInt128 b) => a * b;
    [MethodImpl(NI)] static Int128 Mul128Signed(Int128 a, Int128 b) => a * b;

    // Constants.
    [MethodImpl(NI)] static ulong Const1(out ulong low) => Math.BigMul(0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, out low);
    [MethodImpl(NI)] static ulong Const2(out ulong low) => Math.BigMul(0x8000000000000000UL, 2, out low);
    [MethodImpl(NI)] static ulong Const3(ulong a, out ulong low) => Math.BigMul(a, 0xFFFFFFFF00000001UL, out low);
    [MethodImpl(NI)] static ulong Const4(ulong a, out ulong low) => Math.BigMul(0x10000000000UL, a, out low);
    [MethodImpl(NI)] static long Const5(out long low) => Math.BigMul(-1L, -1L, out low);
    [MethodImpl(NI)] static long Const6(out long low) => Math.BigMul(long.MinValue, long.MinValue, out low);
    [MethodImpl(NI)] static long Const7(long a, out long low) => Math.BigMul(a, -3L, out low);
    [MethodImpl(NI)] static long Const8(long a, out long low) => Math.BigMul(long.MinValue, a, out low);
    [MethodImpl(NI)] static ulong Const9(ulong a, out ulong low) => Math.BigMul(a, 0, out low);
    [MethodImpl(NI)] static ulong Const10(ulong a, out ulong low) => Math.BigMul(1, a, out low);

    // ---- order of evaluation: first factor, second factor, address, then the store ----

    [MethodImpl(NI)] static ulong Step(ulong v, int id) { trace = trace * 10 + id; return v; }
    [MethodImpl(NI)] static int StepIndex(int v, int id) { trace = trace * 10 + id; return v; }
    [MethodImpl(NI)] static ulong[] StepArray(ulong[] v, int id) { trace = trace * 10 + id; return v; }

    [MethodImpl(NI)]
    static ulong Ordered(ulong[] v, ulong a, ulong b)
        => Math.BigMul(Step(a, 1), Step(b, 2), out StepArray(v, 3)[StepIndex(1, 4)]);

    [MethodImpl(NI)]
    static ulong OrderedReads(ulong[] v)
    {
        // The factors are read before the element is written, whatever the index.
        int i = 0;
        return Math.BigMul(v[i++], v[i++], out v[i - 2]);
    }

    [MethodImpl(NI)]
    static long BadIndex(ulong[] v, ulong a, ulong b, int index)
    {
        try
        {
            return (long)Math.BigMul(Step(a, 1), Step(b, 2), out v[StepIndex(index, 3)]);
        }
        catch (IndexOutOfRangeException)
        {
            return -7;
        }
    }

    // ---- many products: 256 x 256 bits ----

    [MethodImpl(NI)]
    static void Multiply(ulong* a, ulong* b, ulong* r)
    {
        for (int i = 0; i < 8; i++) r[i] = 0;
        for (int i = 0; i < 4; i++)
        {
            ulong carry = 0;
            for (int j = 0; j < 4; j++)
            {
                ulong high = Math.BigMul(a[i], b[j], out ulong low);
                low += carry;
                if (low < carry) high++;
                ulong sum = r[i + j] + low;
                if (sum < low) high++;
                r[i + j] = sum;
                carry = high;
            }
            r[i + 4] = carry;
        }
    }

    [MethodImpl(NoOpt)]
    static void RMultiply(ulong* a, ulong* b, ulong* r)
    {
        for (int i = 0; i < 8; i++) r[i] = 0;
        for (int i = 0; i < 4; i++)
        {
            ulong carry = 0;
            for (int j = 0; j < 4; j++)
            {
                ulong high = RefHigh(a[i], b[j], out ulong low);
                low += carry;
                if (low < carry) high++;
                ulong sum = r[i + j] + low;
                if (sum < low) high++;
                r[i + j] = sum;
                carry = high;
            }
            r[i + 4] = carry;
        }
    }

    // ---- driver ----

    static readonly ulong[] Vs =
    {
        0, 1, 2, 3, 0xFFFFFFFFUL, 0x100000000UL, 0x100000001UL, 0x7FFFFFFFFFFFFFFFUL, 0x8000000000000000UL, 0x8000000000000001UL,
        0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFEUL, 0xFFFFFFFF00000000UL, 0x00000000FFFFFFFFUL, 0x0123456789ABCDEFUL,
        0xFEDCBA9876543210UL, 0x5555555555555555UL, 0xAAAAAAAAAAAAAAAAUL, 0xDEADBEEFCAFEBABEUL, 0x000000000000FFFFUL,
        0x8000000080000000UL, 0x7FFFFFFF80000000UL, 10000000000000000000UL, 0xFFFFFFFFFFFFFFFDUL,
    };

    static void Eq(string name, ulong a, ulong b, ulong want, ulong got)
    {
        checks++;
        if (want == got) return;
        fails++;
        if (++shown <= 60)
            Console.WriteLine("FAIL " + name + " (0x" + a.ToString("x") + ", 0x" + b.ToString("x") + ") expected 0x" + want.ToString("x")
                              + " got 0x" + got.ToString("x"));
    }

    static void Pairs()
    {
        Cell cell = new Cell();
        ulong[] vec = new ulong[4];
        ulong* mem = stackalloc ulong[2];
        foreach (ulong a in Vs)
        foreach (ulong b in Vs)
        {
            ulong wl, gl;
            ulong wh = RefHigh(a, b, out wl);
            long sl, gsl;
            long sh = RefHighSigned((long)a, (long)b, out sl);

            ulong gh = Plain(a, b, out gl);
            Eq("high", a, b, wh, gh); Eq("low", a, b, wl, gl);
            long gsh = PlainSigned((long)a, (long)b, out gsl);
            Eq("signed high", a, b, (ulong)sh, (ulong)gsh); Eq("signed low", a, b, (ulong)sl, (ulong)gsl);

            gh = OverFirst(a, b, out gl);
            Eq("out a: high", a, b, wh, gh); Eq("out a: low", a, b, wl, gl);
            gh = OverSecond(a, b, out gl);
            Eq("out b: high", a, b, wh, gh); Eq("out b: low", a, b, wl, gl);
            gsh = OverFirstSigned((long)a, (long)b, out gsl);
            Eq("signed out a: high", a, b, (ulong)sh, (ulong)gsh); Eq("signed out a: low", a, b, (ulong)sl, (ulong)gsl);
            gsh = OverSecondSigned((long)a, (long)b, out gsl);
            Eq("signed out b: high", a, b, (ulong)sh, (ulong)gsh); Eq("signed out b: low", a, b, (ulong)sl, (ulong)gsl);
            gh = OverHigh(a, b, out gl);
            Eq("result over a factor: high", a, b, wh, gh); Eq("result over a factor: low", a, b, wl, gl);

            ulong x = a, y = b;
            gh = OverRef(ref x, b);
            Eq("out ref a: high", a, b, wh, gh); Eq("out ref a: low", a, b, wl, x);
            x = a; y = b;
            gh = OverRefBoth(ref x, ref y);
            Eq("out ref b: high", a, b, wh, gh); Eq("out ref b: low", a, b, wl, y); Eq("out ref b: a kept", a, b, a, x);
            x = a;
            gh = OverRefBoth(ref x, ref x);
            ulong ql;
            ulong qh = RefHigh(a, a, out ql);
            Eq("out ref, same variable: high", a, a, qh, gh); Eq("out ref, same variable: low", a, a, ql, x);

            mem[0] = a; mem[1] = b;
            gh = OverPointer(mem);
            Eq("out p[0]: high", a, b, wh, gh); Eq("out p[0]: low", a, b, wl, mem[0]); Eq("out p[0]: b kept", a, b, b, mem[1]);
            mem[0] = a; mem[1] = b;
            gh = OverPointerSecond(mem);
            Eq("out p[1]: high", a, b, wh, gh); Eq("out p[1]: low", a, b, wl, mem[1]);

            cell.Low = a;
            gh = OverField(cell, b);
            Eq("out field: high", a, b, wh, gh); Eq("out field: low", a, b, wl, cell.Low);
            cell.Low = a; cell.Other = b;
            gh = OverFieldBoth(cell);
            Eq("out other field: high", a, b, wh, gh); Eq("out other field: low", a, b, wl, cell.Other);

            vec[0] = a; vec[1] = b;
            gh = OverElement(vec);
            Eq("out element: high", a, b, wh, gh); Eq("out element: low", a, b, wl, vec[1]);

            staticLow = a;
            gh = OverStatic(b);
            Eq("out static: high", a, b, wh, gh); Eq("out static: low", a, b, wl, staticLow);

            Wide w;
            w.A = a; w.B = b;
            gh = OverStruct(ref w);
            Eq("out struct field: high", a, b, wh, gh); Eq("out struct field: low", a, b, wl, w.A);

            Eq("high only", a, b, wh, HighOnly(a, b));
            Eq("signed high only", a, b, (ulong)sh, (ulong)HighOnlySigned((long)a, (long)b));
            Eq("low only", a, b, wl, LowOnly(a, b));
            Eq("signed low only", a, b, (ulong)sl, (ulong)LowOnlySigned((long)a, (long)b));
            cell.Low = ~wl;
            StoreOnly(a, b, cell);
            Eq("store only", a, b, wl, cell.Low);

            gh = BothKinds(a, b, out gsh, out gl, out gsl);
            Eq("both kinds: high", a, b, wh, gh); Eq("both kinds: signed high", a, b, (ulong)sh, (ulong)gsh);
            Eq("both kinds: low", a, b, wl, gl); Eq("both kinds: signed low", a, b, (ulong)sl, (ulong)gsl);
            gh = BothKindsReversed(a, b, out gsh);
            Eq("both kinds, signed first: sum", a, b, wh + wl + (ulong)sl, gh);
            Eq("both kinds, signed first: signed high", a, b, (ulong)sh, (ulong)gsh);

            gh = Twice(a, b, out gl);
            Eq("twice: high", a, b, wh ^ (wh * 3) ^ (wh * 5), gh); Eq("twice: low", a, b, wl, gl);

            Eq("mixed", a, b, RMixed(a, b, a ^ b), Mixed(a, b, a ^ b));

            UInt128 full = Full(a, b);
            Eq("UInt128: high", a, b, wh, (ulong)(full >> 64)); Eq("UInt128: low", a, b, wl, (ulong)full);
            Int128 sfull = FullSigned((long)a, (long)b);
            Eq("Int128: high", a, b, (ulong)sh, (ulong)(sfull >> 64)); Eq("Int128: low", a, b, (ulong)sl, (ulong)sfull);
            UInt128 big = Mul128(new UInt128(b, a), new UInt128(a, b));
            // (b * 2^64 + a) * (a * 2^64 + b) mod 2^128
            ulong ah, al, bh2, bl2, ch, cl;
            ah = RefHigh(a, b, out al);
            bh2 = RefHigh(a, a, out bl2);
            ch = RefHigh(b, b, out cl);
            Eq("UInt128 * UInt128: low", a, b, al, (ulong)big);
            Eq("UInt128 * UInt128: high", a, b, ah + bl2 + cl, (ulong)(big >> 64));
            Int128 sbig = Mul128Signed((Int128)(long)a, (Int128)(long)b);
            Eq("Int128 * Int128: low", a, b, (ulong)sl, (ulong)sbig);
            Eq("Int128 * Int128: high", a, b, (ulong)sh, (ulong)(sbig >> 64));

            trace = 0;
            vec[1] = 0;
            gh = Ordered(vec, a, b);
            Eq("ordered: high", a, b, wh, gh); Eq("ordered: low", a, b, wl, vec[1]); Eq("ordered: order", a, b, 1234, (ulong)trace);

            vec[0] = a; vec[1] = b;
            gh = OrderedReads(vec);
            Eq("ordered reads: high", a, b, wh, gh); Eq("ordered reads: low", a, b, wl, vec[0]); Eq("ordered reads: b kept", a, b, b, vec[1]);
        }
    }

    static void Singles()
    {
        ulong gl;
        long gsl;
        foreach (ulong a in Vs)
        {
            ulong wl;
            ulong wh = RefHigh(a, a, out wl);
            ulong gh = Square(a, out gl);
            Eq("square: high", a, a, wh, gh); Eq("square: low", a, a, wl, gl);

            wh = RefHigh(a, 0xFFFFFFFF00000001UL, out wl);
            gh = Const3(a, out gl);
            Eq("constant factor: high", a, 3, wh, gh); Eq("constant factor: low", a, 3, wl, gl);
            wh = RefHigh(0x10000000000UL, a, out wl);
            gh = Const4(a, out gl);
            Eq("constant first factor: high", a, 4, wh, gh); Eq("constant first factor: low", a, 4, wl, gl);
            long sl;
            long sh = RefHighSigned((long)a, -3, out sl);
            long gsh = Const7((long)a, out gsl);
            Eq("signed constant factor: high", a, 7, (ulong)sh, (ulong)gsh); Eq("signed constant factor: low", a, 7, (ulong)sl, (ulong)gsl);
            sh = RefHighSigned(long.MinValue, (long)a, out sl);
            gsh = Const8((long)a, out gsl);
            Eq("signed constant first factor: high", a, 8, (ulong)sh, (ulong)gsh); Eq("signed constant first factor: low", a, 8, (ulong)sl, (ulong)gsl);
            gh = Const9(a, out gl);
            Eq("times zero: high", a, 9, 0, gh); Eq("times zero: low", a, 9, 0, gl);
            gh = Const10(a, out gl);
            Eq("times one: high", a, 10, 0, gh); Eq("times one: low", a, 10, a, gl);
        }

        ulong h = Const1(out gl);
        Eq("constants: high", 1, 1, 0xFFFFFFFFFFFFFFFEUL, h); Eq("constants: low", 1, 1, 1, gl);
        h = Const2(out gl);
        Eq("constants: high", 2, 2, 1, h); Eq("constants: low", 2, 2, 0, gl);
        long s = Const5(out gsl);
        Eq("signed constants: high", 5, 5, 0, (ulong)s); Eq("signed constants: low", 5, 5, 1, (ulong)gsl);
        s = Const6(out gsl);
        Eq("signed constants: high", 6, 6, 0x4000000000000000UL, (ulong)s); Eq("signed constants: low", 6, 6, 0, (ulong)gsl);

        ulong[] vec = { 11, 22, 33 };
        trace = 0;
        Eq("bad index: result", 0, 0, unchecked((ulong)-7L), (ulong)BadIndex(vec, 5, 7, 3));
        Eq("bad index: order", 0, 0, 123, (ulong)trace);
        Eq("bad index: nothing stored", 0, 0, 11 + 22 + 33, vec[0] + vec[1] + vec[2]);
        trace = 0;
        Eq("good index: result", 0, 0, 0, (ulong)BadIndex(vec, 5, 7, 2));
        Eq("good index: stored", 0, 0, 35, vec[2]);

        ulong* a4 = stackalloc ulong[4];
        ulong* b4 = stackalloc ulong[4];
        ulong* r8 = stackalloc ulong[8];
        ulong* w8 = stackalloc ulong[8];
        for (int round = 0; round < Vs.Length - 4; round++)
        {
            for (int i = 0; i < 4; i++)
            {
                a4[i] = Vs[round + i] ^ ((ulong)round * 0x9E3779B97F4A7C15UL);
                b4[i] = Vs[Vs.Length - 1 - round - i] + (ulong)i;
            }
            Multiply(a4, b4, r8);
            RMultiply(a4, b4, w8);
            for (int i = 0; i < 8; i++) Eq("256-bit product, word " + i, (ulong)round, 0, w8[i], r8[i]);
        }
    }

    static int Main()
    {
        Pairs();
        Singles();
        Console.WriteLine(fails == 0 ? "ALL PASS (" + checks + " checks)" : "FAILED " + fails + " of " + checks);
        return fails == 0 ? 0 : 1;
    }
}
