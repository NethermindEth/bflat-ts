// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// float and double across exception handling on a target without the F and
// D extensions.
//
// Without floating-point registers a float or a double that is live across
// a call sits in a callee-saved integer register or on the frame. When an
// exception unwinds, the runtime's assembly stubs restore the callee-saved
// integer registers of the frame that catches and call its filter, catch
// and finally funclets; the parts of those stubs that saved and restored
// fs0-fs11 are compiled out. Inside a funclet every floating-point
// operation is again a call of a soft-float routine. What can go wrong: a
// floating-point local of the catching frame that comes back with the value
// some unwound frame left in the register, a stub whose frame layout no
// longer matches once the floating-point saves are gone, a funclet that
// reads a local from the wrong place, a filter that sees a value the
// finally has not produced yet or the other way round, and a value computed
// before a call that throws and used after the catch.
//
// Checks: ten doubles, four floats and two longs of a frame survive an
// exception thrown several frames down, where every frame fills the
// callee-saved registers with its own doubles; the same with the values
// read inside a filter, a catch and a finally, and with a rethrow through
// two catching frames. Then arithmetic in a try, a nested finally, a filter
// and a catch for the three ways through them (no exception, caught, not
// caught here), compared with the exact results; the order in which filters
// and finally blocks see the values; an exception in the middle of an
// expression; a loop with a float counter that throws and catches; a checked
// conversion that throws with floats live; and a return value computed
// before a finally.
//
// The survival checks compare bit patterns; the arithmetic ones compare with
// values computed outside the test with exact rational arithmetic. Each
// failed check prints one line; the run ends with ALL PASS or a FAIL count,
// and the guest returns 0 or 1.
using System;
using System.Runtime.CompilerServices;

#pragma warning disable CS1718 // a value compared with itself, on purpose

class E : Exception
{
    public int Code;
    public E(int c) : base("e") { Code = c; }
}

unsafe class Program
{
    const MethodImplOptions NoInl = MethodImplOptions.NoInlining;
    const ulong Seed = 14695981039346656037UL;

    static int fails;
    static int checks;

    static bool NanD(ulong b) => (b & 0x7FFFFFFFFFFFFFFFUL) > 0x7FF0000000000000UL;
    static bool NanF(uint b) => (b & 0x7FFFFFFFU) > 0x7F800000U;

    [MethodImpl(NoInl)]
    static void CheckD(string name, ulong expected, double got)
    {
        ulong g = *(ulong*)&got;
        checks++;
        if (NanD(expected) ? NanD(g) : g == expected) return;
        fails++;
        Console.WriteLine("FAIL " + name + " expected 0x" + expected.ToString("x16") + " got 0x" + g.ToString("x16"));
    }

    [MethodImpl(NoInl)]
    static void CheckF(string name, uint expected, float got)
    {
        uint g = *(uint*)&got;
        checks++;
        if (NanF(expected) ? NanF(g) : g == expected) return;
        fails++;
        Console.WriteLine("FAIL " + name + " expected 0x" + expected.ToString("x8") + " got 0x" + g.ToString("x8"));
    }

    [MethodImpl(NoInl)]
    static void CheckL(string name, ulong expected, ulong got)
    {
        checks++;
        if (got == expected) return;
        fails++;
        Console.WriteLine("FAIL " + name + " expected 0x" + expected.ToString("x16") + " got 0x" + got.ToString("x16"));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ulong H(ulong h, ulong v) => (h ^ v) * 0x100000001B3UL + (v >> 29);

    [MethodImpl(MethodImplOptions.AggressiveInlining)] static ulong BD(double d) => *(ulong*)&d;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] static ulong BF(float f) => *(uint*)&f;
    [MethodImpl(NoInl)] static double D(ulong bits) => *(double*)&bits;
    [MethodImpl(NoInl)] static float F(uint bits) => *(float*)&bits;

    [MethodImpl(NoInl)] static void Throw(int code) => throw new E(code);

    // ---- values of the catching frame survive the unwinding ----

    static ulong sink;

    [MethodImpl(NoInl)]
    static void Sink(double a, double b, double c, double d, double e, double f, double g, double h, double i,
                     double j)
    {
        sink = H(H(H(H(H(H(H(H(H(H(sink, BD(a)), BD(b)), BD(c)), BD(d)), BD(e)), BD(f)), BD(g)), BD(h)), BD(i)), BD(j));
    }

    // Ten doubles live across calls in every frame down to the throw.
    [MethodImpl(NoInl)]
    static void ThrowDeep(int depth, double a, double b, int code)
    {
        double x0 = a + depth, x1 = b * 1.5, x2 = a - b, x3 = a * b, x4 = a / (b + 1.0), x5 = x0 + x1, x6 = x2 * x3,
               x7 = x4 - x5, x8 = x6 + 1.0, x9 = x7 * 2.0;
        Sink(x0, x1, x2, x3, x4, x5, x6, x7, x8, x9);
        if (depth == 0) Throw(code);
        ThrowDeep(depth - 1, x8, x9, code);
        Sink(x9, x8, x7, x6, x5, x4, x3, x2, x1, x0);
    }

    [MethodImpl(NoInl)]
    static ulong HashAll(ulong* b)
    {
        ulong x = Seed;
        for (int n = 0; n < 10; n++) x = H(x, b[n]);
        for (int n = 10; n < 14; n++) x = H(x, (uint)b[n]);
        return H(H(x, b[14]), b[15]);
    }

    [MethodImpl(NoInl)]
    static ulong Survive(ulong* b, int depth, out int code)
    {
        double v0 = *(double*)(b + 0), v1 = *(double*)(b + 1), v2 = *(double*)(b + 2), v3 = *(double*)(b + 3),
               v4 = *(double*)(b + 4), v5 = *(double*)(b + 5), v6 = *(double*)(b + 6), v7 = *(double*)(b + 7),
               v8 = *(double*)(b + 8), v9 = *(double*)(b + 9);
        float w0 = *(float*)(b + 10), w1 = *(float*)(b + 11), w2 = *(float*)(b + 12), w3 = *(float*)(b + 13);
        long k0 = (long)b[14], k1 = (long)b[15];
        code = 0;
        try
        {
            Sink(v0, v1, v2, v3, v4, v5, v6, v7, v8, v9);
            ThrowDeep(depth, v0, v1, 42);
        }
        catch (E e)
        {
            code = e.Code;
        }
        ulong x = H(Seed, BD(v0));
        x = H(x, BD(v1)); x = H(x, BD(v2)); x = H(x, BD(v3)); x = H(x, BD(v4)); x = H(x, BD(v5)); x = H(x, BD(v6));
        x = H(x, BD(v7)); x = H(x, BD(v8)); x = H(x, BD(v9)); x = H(x, BF(w0)); x = H(x, BF(w1)); x = H(x, BF(w2));
        x = H(x, BF(w3)); x = H(x, (ulong)k0); x = H(x, (ulong)k1);
        return x;
    }

    // No call in the try region besides the one that throws, and no address
    // taken: the values have no reason to be on the frame.
    [MethodImpl(NoInl)]
    static double SurviveSum(double v0, double v1, double v2, double v3, float w0, float w1, int depth)
    {
        double r = 0.0;
        try
        {
            ThrowDeep(depth, v0, v1, 43);
        }
        catch (E)
        {
            r = 1.0;
        }
        // Each operand is a power of two times a small integer, so the sum is exact.
        return v0 + v1 + v2 + v3 + w0 + w1 + r;
    }

    static ulong inFilter, inCatch, inFinally;

    [MethodImpl(NoInl)]
    static bool FilterUse(E e, double v, float w)
    {
        inFilter = H(H(H(Seed, (ulong)e.Code), BD(v)), BF(w));
        return true;
    }

    // The values are read inside the filter, the catch and the finally.
    [MethodImpl(NoInl)]
    static ulong SurviveInFunclets(ulong* b, int depth)
    {
        double v0 = *(double*)(b + 0), v1 = *(double*)(b + 1), v2 = *(double*)(b + 2), v3 = *(double*)(b + 3),
               v4 = *(double*)(b + 4), v5 = *(double*)(b + 5), v6 = *(double*)(b + 6), v7 = *(double*)(b + 7),
               v8 = *(double*)(b + 8), v9 = *(double*)(b + 9);
        float w0 = *(float*)(b + 10), w1 = *(float*)(b + 11), w2 = *(float*)(b + 12), w3 = *(float*)(b + 13);
        long k0 = (long)b[14], k1 = (long)b[15];
        try
        {
            try
            {
                Sink(v0, v1, v2, v3, v4, v5, v6, v7, v8, v9);
                ThrowDeep(depth, v8, v9, 44);
            }
            finally
            {
                inFinally = H(H(H(H(Seed, BD(v6)), BF(w3)), BD(v7)), (ulong)k1);
            }
        }
        catch (E e) when (FilterUse(e, v3, w1) && (v3 == v3 || v3 != v3) && (w1 == w1 || w1 != w1))
        {
            inCatch = H(H(H(H(Seed, BD(v5)), BF(w2)), BD(v4)), (ulong)e.Code);
        }
        ulong x = H(Seed, BD(v0));
        x = H(x, BD(v1)); x = H(x, BD(v2)); x = H(x, BD(v3)); x = H(x, BD(v4)); x = H(x, BD(v5)); x = H(x, BD(v6));
        x = H(x, BD(v7)); x = H(x, BD(v8)); x = H(x, BD(v9)); x = H(x, BF(w0)); x = H(x, BF(w1)); x = H(x, BF(w2));
        x = H(x, BF(w3)); x = H(x, (ulong)k0); x = H(x, (ulong)k1);
        return x;
    }

    static ulong inner;

    // Catches, records its own values and throws again.
    [MethodImpl(NoInl)]
    static void Rethrower(double a, float b, double c, int depth)
    {
        double a2 = D(BD(a) ^ 0x10), c2 = D(BD(c) ^ 0x20);
        float b2 = F((uint)BF(b) ^ 0x40);
        try
        {
            ThrowDeep(depth, a, c, 45);
        }
        catch (E)
        {
            inner = H(H(H(H(H(H(Seed, BD(a)), BF(b)), BD(c)), BD(a2)), BF(b2)), BD(c2));
            throw;
        }
    }

    [MethodImpl(NoInl)]
    static ulong SurviveRethrow(ulong* b, int depth)
    {
        double v0 = *(double*)(b + 0), v1 = *(double*)(b + 1), v2 = *(double*)(b + 2), v3 = *(double*)(b + 3);
        float w0 = *(float*)(b + 10), w1 = *(float*)(b + 11);
        int code = 0;
        try
        {
            Rethrower(v1, w1, v2, depth);
        }
        catch (E e)
        {
            code = e.Code;
        }
        return H(H(H(H(H(H(H(Seed, BD(v0)), BD(v1)), BD(v2)), BD(v3)), BF(w0)), BF(w1)), (ulong)code);
    }

    // ---- arithmetic in a try, a nested finally, a filter and a catch ----

    static double sAcc;
    static float sFacc;

    [MethodImpl(NoInl)]
    static double Handlers(double x, float y, int mode, double limit, out float fout)
    {
        double acc = x;
        float facc = y;
        try
        {
            try
            {
                acc = acc * 1.5 + 0.25;
                facc = facc / 3.0f;
                if (mode == 1) Throw(1);
                acc -= 0.125;
                if (mode == 2) Throw(2);
                facc -= 0.5f;
            }
            finally
            {
                acc = acc * 2.0 - facc;
                facc += 1.0f;
            }
            acc += 1000.0;
        }
        catch (E e) when (e.Code == 1 || acc * 0.5 > limit)
        {
            // The filter ran before the finally above; this runs after it.
            acc = acc / 7.0 + e.Code;
            facc = facc * (float)acc;
        }
        finally
        {
            acc += facc;
            sAcc = acc;
            sFacc = facc;
        }
        fout = facc;
        return acc;
    }

    static void RunHandlers(int row, ulong x, uint y, int mode, ulong limit, ulong acc, uint facc, bool propagates)
    {
        string r = "handlers row " + row;
        bool thrown = false;
        double got = 0;
        float fgot = 0;
        sAcc = 0;
        sFacc = 0;
        try
        {
            got = Handlers(D(x), F(y), mode, D(limit), out fgot);
        }
        catch (E)
        {
            thrown = true;
        }
        CheckL(r + ", the exception leaves the method", propagates ? 1UL : 0, thrown ? 1UL : 0);
        if (!propagates && !thrown)
        {
            CheckD(r + ", double result", acc, got);
            CheckF(r + ", float result", facc, fgot);
        }
        CheckD(r + ", double in the last finally", acc, sAcc);
        CheckF(r + ", float in the last finally", facc, sFacc);
    }

    // ---- the order of filters and finally blocks ----

    static double[] log = new double[16];
    static int logN;
    static int logAtFilter;

    [MethodImpl(NoInl)] static void Log(double v) => log[logN++] = v;

    [MethodImpl(NoInl)]
    static void Level(int n, double v)
    {
        double w = v * 1.5;
        try
        {
            if (n == 0) Throw(3);
            Level(n - 1, w + 0.5);
        }
        finally
        {
            Log(w - n);
        }
    }

    [MethodImpl(NoInl)] static bool NoteFilter(double seen) { logAtFilter = logN; Log(seen); return true; }

    [MethodImpl(NoInl)]
    static void RunOrder()
    {
        logN = 0;
        double top = D(0x3FF0000000000000UL);
        try
        {
            Level(3, top);
        }
        catch (E) when (NoteFilter(top + 0.25))
        {
            Log(top + 0.75);
        }
        // The filter first, then the finally blocks from the innermost frame out, then the catch.
        CheckL("entries logged before the filter", 0, (ulong)logAtFilter);
        CheckL("entries logged", 6, (ulong)logN);
        CheckD("logged by the filter", 0x3FF4000000000000UL, log[0]);            // 1.25
        CheckD("logged by the innermost finally", 0x4021400000000000UL, log[1]); // 8.625
        CheckD("logged by the second finally", 0x4011000000000000UL, log[2]);    // 4.25
        CheckD("logged by the third finally", 0x3FF0000000000000UL, log[3]);     // 1.0
        CheckD("logged by the outermost finally", 0xBFF8000000000000UL, log[4]); // -1.5
        CheckD("logged by the catch", 0x3FFC000000000000UL, log[5]);             // 1.75
    }

    // ---- an exception in the middle of an expression ----

    [MethodImpl(NoInl)] static double MaybeThrow(double v, bool t) { if (t) Throw(7); return v; }

    [MethodImpl(NoInl)]
    static double Partial(double a, double b, double c, double d, bool t)
    {
        double p = a * b;
        double q = 0.0;
        try
        {
            q = p + MaybeThrow(c, t) * d;
        }
        catch (E)
        {
            q = p - d;
        }
        return q * 0.5 + p;
    }

    // ---- a loop with a float counter that throws and catches ----

    [MethodImpl(NoInl)]
    static float LoopCatch(float step, float limit, out int caught)
    {
        float sum = 0f;
        caught = 0;
        for (float f = 0f; f < 10f; f += step)
        {
            try
            {
                if (f > limit) Throw(1);
                sum += f * 0.5f;
            }
            catch (E)
            {
                sum -= f;
                caught++;
            }
            finally
            {
                sum += 0.25f;
            }
        }
        return sum;
    }

    static void RunLoop(int row, uint step, uint limit, uint sum, int caught)
    {
        float got = LoopCatch(F(step), F(limit), out int c);
        CheckF("loop row " + row + ", sum", sum, got);
        CheckL("loop row " + row + ", exceptions caught", (ulong)caught, (ulong)c);
    }

    // ---- a checked conversion that may throw, with floats live ----

    [MethodImpl(NoInl)]
    static double CheckedIn(double a, float b, double big)
    {
        double keep = a * 3.0;
        float fk = b + 0.5f;
        int r;
        try
        {
            r = checked((int)big);
        }
        catch (OverflowException)
        {
            r = -1;
        }
        return keep + fk + r;
    }

    // ---- a return value computed before a finally ----

    static double sProduct;

    [MethodImpl(NoInl)]
    static double RetFinally(double a, double b)
    {
        try
        {
            return a + b;
        }
        finally
        {
            sProduct = a * b;
            Log(a - b);
        }
    }

    static void RunRetFinally(ulong a, ulong b, ulong sum, ulong product, ulong difference)
    {
        logN = 0;
        CheckD("return value computed before a finally", sum, RetFinally(D(a), D(b)));
        CheckD("product stored in the finally", product, sProduct);
        CheckD("difference logged in the finally", difference, log[0]);
    }

    static readonly ulong[] Pattern =
    {
        0x3FF8000000000000UL, 0xC0091EB851EB851FUL, 0x7FF8DEADBEEF0001UL, 0x8000000000000000UL,
        0x0000000000000001UL, 0xFFEFFFFFFFFFFFFFUL, 0x400921FB54442D18UL, 0x123456789ABCDEF0UL,
        0xFEDCBA9876543210UL, 0x7FF0000000000000UL,
        0x3FC00000UL, 0xC0490FDBUL, 0x7FC0BEEFUL, 0x80000001UL,
        0x0123456789ABCDEFUL, 0xF0E1D2C3B4A59687UL,
    };

    static void RunSurvive()
    {
        ulong* b = stackalloc ulong[16];
        for (int round = 0; round < 8; round++)
        {
            for (int n = 0; n < 10; n++) b[n] = Pattern[(n + round) % 10] ^ ((ulong)round << 17);
            for (int n = 10; n < 14; n++) b[n] = Pattern[10 + (n + round) % 4] ^ ((ulong)round << 3);
            b[14] = Pattern[14] + (ulong)round;
            b[15] = Pattern[15] * (ulong)(round + 1);
            int depth = round % 4;
            string r = "round " + round + ", thrown " + depth + " frames down, ";

            ulong want = HashAll(b);
            CheckL(r + "values after the catch", want, Survive(b, depth, out int code));
            CheckL(r + "exception code", 42, (ulong)code);

            inFilter = inCatch = inFinally = 0;
            CheckL(r + "values after a filter, a catch and a finally", want, SurviveInFunclets(b, depth));
            CheckL(r + "values in the filter", H(H(H(Seed, 44), b[3]), (uint)b[11]), inFilter);
            CheckL(r + "values in the catch", H(H(H(H(Seed, b[5]), (uint)b[12]), b[4]), 44), inCatch);
            CheckL(r + "values in the finally", H(H(H(H(Seed, b[6]), (uint)b[13]), b[7]), b[15]), inFinally);

            inner = 0;
            CheckL(r + "values after a rethrow",
                   H(H(H(H(H(H(H(Seed, b[0]), b[1]), b[2]), b[3]), (uint)b[10]), (uint)b[11]), 45),
                   SurviveRethrow(b, depth));
            CheckL(r + "values in the frame that rethrows",
                   H(H(H(H(H(H(Seed, b[1]), (uint)b[11]), b[2]), b[1] ^ 0x10), (uint)b[11] ^ 0x40), b[2] ^ 0x20), inner);

            // 3 + 0.5 + 96 + (-0.25) + 1.5f + (-8f) + 1 = 93.75
            CheckD(r + "values in registers after the catch", 0x4057700000000000UL,
                   SurviveSum(D(0x4008000000000000UL), D(0x3FE0000000000000UL), D(0x4058000000000000UL),
                              D(0xBFD0000000000000UL), F(0x3FC00000U), F(0xC1000000U), depth));
        }
    }

    static int Main()
    {
        RunSurvive();
        RunOrder();
        RunHandlers(0, 0x3fb999999999999aUL, 0x3e99999aU, 0, 0x0000000000000000UL, 0x408f4c66666a6666UL, 0x3f19999aU, false);
        RunHandlers(1, 0x3fb999999999999aUL, 0x3e99999aU, 1, 0x0000000000000000UL, 0x40027ae14cc57c58UL, 0x3f9ae148U, false);
        RunHandlers(2, 0x3fb999999999999aUL, 0x3e99999aU, 2, 0x0000000000000000UL, 0x4011570a3d3e2be3UL, 0x40115362U, false);
        RunHandlers(3, 0x3fb999999999999aUL, 0x3e99999aU, 2, 0x4059000000000000UL, 0x3ff8ccccd2cccccdUL, 0x3f8ccccdU, true);
        RunHandlers(4, 0xc01ecccccccccccdUL, 0x501502f9U, 1, 0x0000000000000000UL, 0xc3b6073a401c6217UL, 0xddb039d2U, false);
        RunHandlers(5, 0xc01ecccccccccccdUL, 0x501502f9U, 2, 0xc1cdcd6500000000UL, 0xc3b6073a401c6217UL, 0xddb039d2U, false);
        RunHandlers(6, 0xc01ecccccccccccdUL, 0x501502f9U, 2, 0x41cdcd6500000000UL, 0xc036d99998000000UL, 0x4f46aea1U, true);
        RunHandlers(7, 0x7e37e43c8800759cUL, 0xc0200000U, 0, 0x0000000000000000UL, 0x7e51eb2d66005835UL, 0xbeaaaaa8U, false);
        RunHandlers(8, 0x7e37e43c8800759cUL, 0xc0200000U, 1, 0x0000000000000000UL, 0x7ff0000000000000UL, 0x7f800000U, false);
        RunHandlers(9, 0x405edd2f1a9fbe77UL, 0x3a83126fU, 2, 0x4057000000000000UL, 0x405b7a2e4f1c2e33UL, 0x425bdad3U, false);
        RunHandlers(10, 0x405edd2f1a9fbe77UL, 0x3a83126fU, 2, 0x4057400000000000UL, 0x407739e353f150d9UL, 0x3f800aecU, true);
        RunHandlers(11, 0x4008000000000000UL, 0x40e00000U, 0, 0x0000000000000000UL, 0x408f920000000000UL, 0x40355555U, false);
        CheckD("exception in the middle of an expression, row 0", 0xbffc7ae147ae147bUL, Partial(D(0x3fb999999999999aUL), D(0x3fe6666666666666UL), D(0x3ff4cccccccccccdUL), D(0xc007333333333333UL), false));
        CheckD("exception in the middle of an expression, row 1", 0x3ff8e147ae147ae1UL, Partial(D(0x3fb999999999999aUL), D(0x3fe6666666666666UL), D(0x3ff4cccccccccccdUL), D(0xc007333333333333UL), true));
        CheckD("exception in the middle of an expression, row 2", 0x3ffaa3d70a3d70a4UL, Partial(D(0x6974e718d7d7625aUL), D(0x16687e92154ef7acUL), D(0x400a666666666666UL), D(0x3fb999999999999aUL), false));
        CheckD("exception in the middle of an expression, row 3", 0x3ff7333333333333UL, Partial(D(0x6974e718d7d7625aUL), D(0x16687e92154ef7acUL), D(0x400a666666666666UL), D(0x3fb999999999999aUL), true));
        RunLoop(0, 0x3fc00000U, 0x40800000U, 0xc1b80000U, 4);
        RunLoop(1, 0x3dcccccdU, 0x40a1999aU, 0xc38b5ffbU, 49);
        RunLoop(2, 0x3f333333U, 0x42c80000U, 0x4221ffffU, 0);
        RunLoop(3, 0x40533333U, 0xbf800000U, 0xc1966666U, 4);
        CheckD("checked conversion that may throw, row 0", 0x40c81d0ccccce666UL, CheckedIn(D(0x3fb999999999999aUL), F(0x3e99999aU), D(0x40c81cf333333333UL)));
        CheckD("checked conversion that may throw, row 1", 0x3fb99999ccccccd0UL, CheckedIn(D(0x3fb999999999999aUL), F(0x3e99999aU), D(0x41e65a0bc0000000UL)));
        CheckD("checked conversion that may throw, row 2", 0xc030ffbe77000000UL, CheckedIn(D(0xc016000000000000UL), F(0x3a83126fU), D(0x7ff8000000000000UL)));
        CheckD("checked conversion that may throw, row 3", 0xc1dfffffff3fff82UL, CheckedIn(D(0x3ee4f8b588e368f1UL), F(0x40200000U), D(0xc1e00000001ccccdUL)));
        CheckD("checked conversion that may throw, row 4", 0x4000000fba8826abUL, CheckedIn(D(0x3ee4f8b588e368f1UL), F(0x40200000U), D(0xc1e0000000200000UL)));
        RunRetFinally(0x3fb999999999999aUL, 0x3fe6666666666666UL, 0x3fe9999999999999UL, 0x3fb1eb851eb851ebUL, 0xbfe3333333333333UL);

        Console.WriteLine("checks " + checks);
        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails == 0 ? 0 : 1;
    }
}
