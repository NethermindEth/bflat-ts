// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Results that must not depend on the block weights the JIT invents for a
// method without profile data.
//
// Without a profile every block of a method has the same weight. For a zkVM
// guest the JIT can synthesize weights instead (JitZkSynthesizeCounts): loop
// bodies are heavy, loop exits, arms that return and arms that throw are
// light, a plain condition is 50/50. The weights steer where values are
// spilled and reloaded, which registers survive calls, how blocks are laid
// out, which calls are inlined and which loops are cloned. They must change
// only the cost of the code. What can go wrong:
//
//   - a value kept in a register across a call that sits in an arm the
//     weights call cold: when the arm does run, the value must still be
//     saved and restored. The cases here run the "cold" arms often: the arm
//     before a return, before a throw, on the way out of a loop, in a
//     handler, with twenty values alive across it;
//   - the synthesis itself on flow it does not expect: a loop entered in two
//     places, deep nests left by goto, switches, try/finally inside loops,
//     filters, a loop that only ends by return;
//   - layout: blocks moved out of line must keep their fall-through and
//     their handlers.
//
// Every case is a method that is not inlined and returns one number. The
// expected numbers are constants taken from a build with -O0, where the JIT
// assigns no weights and moves no blocks; a build with the weights on or off
// has to print the same.
//
// Each case prints one line on failure; the run ends with ALL PASS or a FAIL
// count, and the guest returns the number of failures.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

static class Program
{
    const MethodImplOptions NoInl = MethodImplOptions.NoInlining;
    static int fails;
    static long state;

    static void Check(string name, long expected, long got)
    {
        if (expected != got)
        {
            fails++;
            Console.WriteLine("FAIL " + name + " expected " + expected + " got " + got);
        }
    }

    [MethodImpl(NoInl)] static int Opaque(int x) => x;

    // Calls that change every register a call may change.
    [MethodImpl(NoInl)]
    static long Mix(long a, long b)
    {
        state = state * 31 + a - b;
        return (a ^ (b << 7)) + (state & 0xFF);
    }

    [MethodImpl(NoInl)]
    static void Touch(ref long a, out long b, long c)
    {
        b = a + c;
        a = a * 3 + state % 7;
        state += c;
    }

    [DoesNotReturn]
    [MethodImpl(NoInl)]
    static void Fail(long code) => throw new InvalidOperationException(code.ToString());

    // ---- twenty values alive across a call in an arm that looks cold ---------------
    [MethodImpl(NoInl)]
    static long LiveAcrossRareCall(int n, int seed)
    {
        long v0 = seed, v1 = seed + 1, v2 = seed + 2, v3 = seed + 3, v4 = seed + 4, v5 = seed + 5, v6 = seed + 6, v7 = seed + 7, v8 = seed + 8, v9 = seed + 9;
        long w0 = seed * 3, w1 = seed * 5, w2 = seed * 7, w3 = seed * 11, w4 = seed * 13, w5 = seed * 17, w6 = seed * 19, w7 = seed * 23, w8 = seed * 29, w9 = seed * 31;
        for (int i = 0; i < n; i++)
        {
            v0 += w9 ^ i; v1 += v0 >> 3; v2 ^= v1 + w0; v3 += v2 & 0xFFFF; v4 -= v3; v5 += v4 ^ w1; v6 += v5 >> 1; v7 ^= v6; v8 += v7 + w2; v9 -= v8 >> 2;
            if ((v3 & 3) == 0)
            {
                // Runs on a quarter of the iterations.
                w0 = Mix(v0, w0); w1 = Mix(v1, w1);
            }
            if (v5 == 0x123456789) Fail(v5);
            if ((i & 127) == 127)
            {
                Touch(ref w3, out w4, v9);
                if (w4 == long.MinValue) return -1;
            }
            w5 += v0 ^ v9; w6 ^= w5 + v1; w7 += w6 >> 5; w8 ^= w7 + v2; w9 += w8 & 0xFFF;
        }
        return v0 + v1 * 3 + v2 * 5 + v3 * 7 + v4 * 11 + v5 * 13 + v6 * 17 + v7 * 19 + v8 * 23 + v9 * 29 +
               w0 * 31 + w1 * 37 + w2 * 41 + w3 * 43 + w4 * 47 + w5 * 53 + w6 * 59 + w7 * 61 + w8 * 67 + w9 * 71;
    }

    // The arm before a return and the arm that leaves the loop, both taken.
    [MethodImpl(NoInl)]
    static long LiveAcrossExit(long[] data, long limit)
    {
        long a = 1, b = 2, c = 3, d = 4, e = 5, f = 6, g = 7, h = 8, k = 9, m = 10, p = 11, q = 12;
        for (int i = 0; i < data.Length; i++)
        {
            long x = data[i];
            a += x; b ^= a; c += b >> 2; d -= c; e += d ^ x; f += e & 255; g ^= f; h += g; k -= h >> 3; m += k; p ^= m; q += p & 15;
            if (x < 0)
            {
                long r = Mix(a, x);
                return r + a + b + c + d + e + f + g + h + k + m + p + q;
            }
            if (x > limit)
            {
                a = Mix(b, c);
                break;
            }
        }
        long t = Mix(d, e);
        return t ^ (a + b * 2 + c * 3 + d * 4 + e * 5 + f * 6 + g * 7 + h * 8 + k * 9 + m * 10 + p * 11 + q * 12);
    }

    // Values used in a handler and after it.
    [MethodImpl(NoInl)]
    static long LiveAcrossThrow(int n)
    {
        long a = 3, b = 5, c = 7, d = 11, e = 13, f = 17, g = 19, h = 23;
        long caught = 0;
        for (int i = 0; i < n; i++)
        {
            a += b ^ i; b += c >> 1; c ^= d + a; d += e; e -= f ^ b; f += g & 63; g ^= h + i; h += a >> 4;
            try
            {
                if ((i % 5) == 4) Fail(a & 0xFFFF);
                if ((i % 7) == 6) throw new ArgumentException("seven");
                b = Mix(b, i);
            }
            catch (InvalidOperationException ex)
            {
                caught += ex.Message.Length + (c & 7);
                d = Mix(d, e);
            }
            catch (ArgumentException) when ((g & 1) == 0)
            {
                caught += 1000;
            }
            catch (ArgumentException)
            {
                caught += 100000;
                f = Mix(f, g);
            }
            finally
            {
                h ^= caught;
            }
        }
        return a + b * 3 + c * 5 + d * 7 + e * 11 + f * 13 + g * 17 + h * 19 + caught * 23;
    }

    // ---- flow the synthesis has to cope with ------------------------------------------
    // A loop entered at its top and in its middle.
    [MethodImpl(NoInl)]
    static long TwoEntries(int n, bool middle)
    {
        long s = 0;
        int i = 0;
        if (middle) goto Middle;
    Top:
        s += i * 3;
        if ((s & 1) == 0) s = Mix(s, i);
    Middle:
        s ^= i + 11;
        i++;
        if (i < n) goto Top;
        return s;
    }

    [MethodImpl(NoInl)]
    static long DeepNest(int n)
    {
        long s = 0;
        for (int a = 0; a < n; a++)
        {
            for (int b = 0; b < n; b++)
            {
                if (b == a) continue;
                for (int c = 0; c < n; c++)
                {
                    if (c > b + 2) break;
                    for (int d = 0; d < n; d++)
                    {
                        s += a * 1000 + b * 100 + c * 10 + d;
                        if (((a + b + c + d) & 7) == 7) goto NextB;
                        if (s > 400000000) goto Done;
                        if (d == c) s = Mix(s, d);
                    }
                }
            NextB:
                s ^= b;
            }
        }
    Done:
        return s;
    }

    [MethodImpl(NoInl)]
    static long Switches(int n)
    {
        long s = 1;
        for (int i = 0; i < n; i++)
        {
            switch (i % 11)
            {
                case 0: s += i; break;
                case 1: s ^= i << 3; goto case 5;
                case 2: s = Mix(s, i); break;
                case 3: continue;
                case 4: s -= 7; goto default;
                case 5: s += s >> 3; break;
                case 6: if ((s & 1) != 0) goto case 0; s++; break;
                case 7: s *= 3; break;
                default: s ^= 0x55; break;
            }
            switch (s & 0xF000)
            {
                case 0x1000: s += 1; break;
                case 0x8000: s = Mix(s, 8); break;
                case 0xF000: s -= 3; break;
            }
        }
        return s;
    }

    [MethodImpl(NoInl)]
    static long FinallyInLoops(int n)
    {
        long s = 0, fin = 0;
        for (int i = 0; i < n; i++)
        {
            try
            {
                if ((i & 3) == 1) continue;
                try
                {
                    if ((i & 15) == 14) break;
                    if ((i % 6) == 5) Fail(i);
                    s += i;
                }
                finally
                {
                    fin += 1;
                    if ((i & 1) == 0) s = Mix(s, fin);
                }
            }
            catch (InvalidOperationException)
            {
                s ^= 0x1000;
                if (i > n) throw;
            }
            finally
            {
                fin += 100;
            }
        }
        return s * 1000003 + fin;
    }

    // Only a return ends the loop.
    [MethodImpl(NoInl)]
    static long OnlyReturn(long x)
    {
        long steps = 0, acc = x;
        while (true)
        {
            if (x == 1) return steps * 1000003 + acc;
            if ((x & 1) == 0) x >>= 1;
            else { x = x * 3 + 1; acc = Mix(acc, x); }
            steps++;
            if (steps > 100000) Fail(steps);
        }
    }

    [MethodImpl(NoInl)]
    static long ShortCircuit(int[] a)
    {
        long s = 0;
        int i = 0;
        do
        {
            int x = a[i];
            if ((x > 3 && x < 40) || (x < -5 && (x & 1) == 0) || x == 77 && i != 0)
                s += x;
            else if (x == 0 || (i > 2 && a[i - 1] == x))
                s = Mix(s, x);
            else
                s -= i;
            i++;
        } while (i < a.Length && (s > -1000000 || a[i] != 0) && !(s == 12345 && i == 3));
        return s * 100 + i;
    }

    [MethodImpl(NoInl)]
    static long Tree(long x, int depth)
    {
        if (depth == 0) return x & 0xFF;
        if ((x & 1) == 0)
        {
            if ((x & 2) == 0) return Tree(x / 3 + depth, depth - 1) + 1;
            long y = Mix(x, depth);
            return Tree(y >> 2, depth - 1) ^ x;
        }
        if ((x & 4) != 0) return Tree(x - 5, depth - 1) + Tree(x >> 3, depth - 1);
        if (x < 0) return -Tree(-x, depth - 1);
        return Tree(x * 7 + 1, depth - 1) - depth;
    }

    static int Main()
    {
        state = 1;
        Check("live across a rare call", 2541757117867131706L, LiveAcrossRareCall(3000, Opaque(17)));
        long[] data = new long[300];
        for (int i = 0; i < data.Length; i++) data[i] = (i * 7919L) % 1009;
        Check("live across a loop exit, runs out", -4297402401694L, LiveAcrossExit(data, 5000));
        Check("live across a loop exit, breaks", -813028378L, LiveAcrossExit(data, 1000));
        data[150] = -9;
        Check("live across a loop exit, returns", -2872043624L, LiveAcrossExit(data, 5000));
        Check("live across a throw", -8998176130052926330L, LiveAcrossThrow(400));
        Check("loop with two entries, top", 363159L, TwoEntries(500, false));
        Check("loop with two entries, middle", 375649L, TwoEntries(500, true));
        Check("deep nest", 5728606L, DeepNest(Opaque(14)));
        Check("switches", -8775803282733251313L, Switches(5000));
        Check("finally in loops", 92042277637L, FinallyInLoops(200));
        Check("finally in loops, no break", 243975733235L, FinallyInLoops(13));
        Check("loop that only returns", 471230575L, OnlyReturn(Opaque(27)) + OnlyReturn(Opaque(97)) * 3 + OnlyReturn(1));
        Check("short circuit", 57714L, ShortCircuit(new[] { 5, -6, 77, 0, 0, 41, 39, -7, 77, 2, 2, 3, 0, -8 }));
        Check("decision tree", 586835651L, Tree(Opaque(123456789), 12) + Tree(-Opaque(987654321), 11));
        Check("state", -4681280041213177395L, state);

        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails;
    }
}
