// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// A P/Invoke that the compiler may emit as a single CSR write.
//
// JitZkCsrPInvokes lists P/Invokes with one argument, by method name, that
// are emitted as `csrrs x0, csr, a0` in place of the call. Such a call
// changes no register, so the register allocator keeps values in
// caller-saved registers across it; only the argument still has to be in
// a0. A value that sits in a0 before the call, or a value the allocator
// wrongly believes safe, is what can break.
//
// The P/Invoke here is csr_probe. Without the option it is strlen with the
// result dropped, so the block it gets always holds a zero byte. Build with
//     --codegenopt JitZkCsrPInvokes=csr_probe:<csr>
// to turn it into the CSR write; <csr> (hex) has to be a CSR that the
// target lets a program set bits in with any operand, 1 (fflags) under
// qemu-user. The test passes both ways: the probe has no visible effect,
// and every check is on the values around it.
//
// Cases: values in the argument registers, a0 included, across the probe;
// sixteen values across it; a block that is stored to before the probe and
// loaded from after it, in a loop without any other call; probes mixed
// with real calls and with a two-argument P/Invoke of the same name, which
// must stay a call; an object reference in a0; probes in try, finally and
// catch; an exception through frames whose only calls before the throw are
// probes.
//
// Each check prints one line; the run ends with ALL PASS or a FAIL count,
// and the guest returns the number of failures.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class E : Exception { public int Code; public E(int c) : base("e") { Code = c; } }

unsafe class Holder { public long A; public byte* P; public long B; }

unsafe class Program
{
    [DllImport("*", EntryPoint = "strlen")] static extern void csr_probe(byte* block);
    [DllImport("*", EntryPoint = "strnlen")] static extern nuint csr_probe(byte* block, nuint max);

    const MethodImplOptions NoOpt = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;
    const long Mult = unchecked((long)0x9E3779B97F4A7C15UL);

    static int fails;

    static void Check(string name, bool ok)
    {
        Console.WriteLine((ok ? "ok   " : "FAIL ") + name);
        if (!ok) fails++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Mix(int seed, int i) => (seed + 1) * Mult + i * 0x100000001B3L;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Thrower(int code)
    {
        if (code >= 0) throw new E(code);
        return 0;
    }

    // ---- values in the argument registers ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Only(byte* p) => csr_probe(p);

    // x arrives in a0, where the argument of the probe has to go.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long KeepFirst(long x, byte* p)
    {
        csr_probe(p);
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long KeepTwo(long x, long y, byte* p)
    {
        csr_probe(p);
        return x * 3 + y;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte* SameRegister(byte* p)
    {
        csr_probe(p);
        return p + 1;
    }

    // Every argument register is live across the probe.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Eight(long a, long b, long c, long d, long e, long f, long g, byte* p)
    {
        csr_probe(p);
        long r = a ^ b * 3 ^ c * 5 ^ d * 7 ^ e * 9 ^ f * 11 ^ g * 13;
        csr_probe(p + 1);
        return r + p[2] + (a + g);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long FromField(Holder h)
    {
        csr_probe(h.P);
        long r = h.A * 3;
        csr_probe(h.P + 1);
        return r + h.B;
    }

    static void Arguments(byte* p)
    {
        bool ok = true;
        Holder h = new Holder { P = p };
        for (int seed = 0; seed < 30; seed++)
        {
            long a = Mix(seed, 0), b = Mix(seed, 1), c = Mix(seed, 2), d = Mix(seed, 3);
            long e = Mix(seed, 4), f = Mix(seed, 5), g = Mix(seed, 6);
            Only(p + (seed & 7));
            ok &= KeepFirst(a, p) == a;
            ok &= KeepTwo(a, b, p) == a * 3 + b;
            ok &= SameRegister(p + seed) == p + seed + 1;
            ok &= Eight(a, b, c, d, e, f, g, p)
                  == (a ^ b * 3 ^ c * 5 ^ d * 7 ^ e * 9 ^ f * 11 ^ g * 13) + p[2] + (a + g);
            h.A = a; h.B = b;
            ok &= FromField(h) == a * 3 + b;
        }
        Check("argument registers", ok);
    }

    // ---- sixteen values across the probe ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Live16(long x, long y, byte* p)
    {
        long v0 = x;
        long v1 = v0 * 31 + y + 1, v2 = v1 * 31 + y + 2, v3 = v2 * 31 + y + 3, v4 = v3 * 31 + y + 4;
        long v5 = v4 * 31 + y + 5, v6 = v5 * 31 + y + 6, v7 = v6 * 31 + y + 7, v8 = v7 * 31 + y + 8;
        long v9 = v8 * 31 + y + 9, v10 = v9 * 31 + y + 10, v11 = v10 * 31 + y + 11, v12 = v11 * 31 + y + 12;
        long v13 = v12 * 31 + y + 13, v14 = v13 * 31 + y + 14, v15 = v14 * 31 + y + 15;
        csr_probe(p);
        long r = v0 ^ v1 * 3 ^ v2 * 5 ^ v3 * 7 ^ v4 * 9 ^ v5 * 11 ^ v6 * 13 ^ v7 * 15;
        csr_probe(p + 3);
        return r ^ v8 * 17 ^ v9 * 19 ^ v10 * 21 ^ v11 * 23 ^ v12 * 25 ^ v13 * 27 ^ v14 * 29 ^ v15 * 31;
    }

    [MethodImpl(NoOpt)]
    static long Live16Ref(long x, long y)
    {
        long v = x, r = x;
        for (int i = 1; i < 16; i++)
        {
            v = v * 31 + y + i;
            r ^= v * (2 * i + 1);
        }
        return r;
    }

    // ---- a block stored before the probe and loaded after it ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Absorb(ulong* st, int rounds, long x)
    {
        long acc = 0;
        for (int r = 0; r < rounds; r++)
        {
            for (int i = 0; i < 8; i++)
            {
                x = x * 6364136223846793005L + 1442695040888963407L;
                st[i] = (ulong)x;
            }
            csr_probe((byte*)st);
            for (int i = 0; i < 8; i++) acc = acc * 31 + (long)st[i];
        }
        return acc ^ x;
    }

    [MethodImpl(NoOpt)]
    static long AbsorbRef(int rounds, long x)
    {
        long acc = 0;
        for (int r = 0; r < rounds; r++)
            for (int i = 0; i < 8; i++)
            {
                x = x * 6364136223846793005L + 1442695040888963407L;
                acc = acc * 31 + x;
            }
        return acc ^ x;
    }

    // ---- probes among real calls ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Mixed(byte* p, int n, int seed)
    {
        long a = Mix(seed, 0), b = Mix(seed, 1), c = Mix(seed, 2);
        long acc = 0;
        for (int i = 0; i < n; i++)
        {
            csr_probe(p + (i & 7));
            acc += (long)csr_probe(p + (i & 15), 100);
            if ((i & 3) == 0) acc ^= Mix(seed, i);
            csr_probe(p);
            acc = acc * 3 + a + (b ^ c);
        }
        return acc;
    }

    [MethodImpl(NoOpt)]
    static long MixedRef(int len, int n, int seed)
    {
        long acc = 0;
        for (int i = 0; i < n; i++)
        {
            acc += len - (i & 15);
            if ((i & 3) == 0) acc ^= Mix(seed, i);
            acc = acc * 3 + Mix(seed, 0) + (Mix(seed, 1) ^ Mix(seed, 2));
        }
        return acc;
    }

    // ---- probes in try, finally and catch ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long TryProbe(byte* p, int code, int seed)
    {
        long a = Mix(seed, 0), b = Mix(seed, 1), c = Mix(seed, 2), d = Mix(seed, 3);
        long r = 0;
        try
        {
            csr_probe(p);
            r += a;
            try
            {
                csr_probe(p + 1);
                Thrower(code);
                r += b;
            }
            finally
            {
                csr_probe(p + 2);
                r = r * 3 + c;
            }
        }
        catch (E e)
        {
            csr_probe(p + 3);
            r = r * 5 + e.Code;
        }
        csr_probe(p);
        return r + d;
    }

    [MethodImpl(NoOpt)]
    static long TryProbeRef(int code, int seed)
    {
        long r = Mix(seed, 0);
        if (code < 0) r += Mix(seed, 1);
        r = r * 3 + Mix(seed, 2);
        if (code >= 0) r = r * 5 + code;
        return r + Mix(seed, 3);
    }

    // ---- an exception through frames with probes ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Through(int depth, int code, long x, byte* p)
    {
        long v0 = x * 3 + 1, v1 = v0 * 5 + depth, v2 = v1 * 7 + x, v3 = v2 * 9 + 4, v4 = v3 * 11 + x, v5 = v4 * 13 + 6;
        csr_probe(p + (depth & 7));
        long r = depth == 0 ? Thrower(code) : Through(depth - 1, code, x + 1, p);
        csr_probe(p);
        return r + (v0 ^ v1 ^ v2 ^ v3 ^ v4 ^ v5);
    }

    [MethodImpl(NoOpt)]
    static long ThroughRef(int depth, long x)
    {
        long v0 = x * 3 + 1, v1 = v0 * 5 + depth, v2 = v1 * 7 + x, v3 = v2 * 9 + 4, v4 = v3 * 11 + x, v5 = v4 * 13 + 6;
        long r = depth == 0 ? 0 : ThroughRef(depth - 1, x + 1);
        return r + (v0 ^ v1 ^ v2 ^ v3 ^ v4 ^ v5);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool CatchKeep(int depth, int code, int seed, byte* p)
    {
        long k0 = Mix(seed, 0), k1 = Mix(seed, 1), k2 = Mix(seed, 2), k3 = Mix(seed, 3);
        long k4 = Mix(seed, 4), k5 = Mix(seed, 5);
        int got = -1;
        long r = 0;
        try
        {
            csr_probe(p);
            r = Through(depth, code, k0, p);
        }
        catch (E e)
        {
            got = e.Code;
        }
        csr_probe(p + 1);
        if ((k0 ^ k1 * 3 ^ k2 * 5 ^ k3 * 7 ^ k4 * 9 ^ k5 * 11)
            != (Mix(seed, 0) ^ Mix(seed, 1) * 3 ^ Mix(seed, 2) * 5 ^ Mix(seed, 3) * 7 ^ Mix(seed, 4) * 9 ^ Mix(seed, 5) * 11))
            return false;
        if (code >= 0) return got == code;
        return got == -1 && r == ThroughRef(depth, k0);
    }

    static int Main()
    {
        const int len = 47;
        byte* p = stackalloc byte[64];
        for (int i = 0; i < len; i++) p[i] = (byte)('a' + i * 7 % 23);
        p[len] = 0;
        ulong* st = stackalloc ulong[9];
        st[8] = 0;

        Arguments(p);

        bool ok = true;
        for (int seed = 0; seed < 30; seed++)
            ok &= Live16(Mix(seed, 0), Mix(seed, 1), p) == Live16Ref(Mix(seed, 0), Mix(seed, 1));
        Check("sixteen values", ok);

        ok = true;
        for (int rounds = 1; rounds < 40; rounds += 3)
            ok &= Absorb(st, rounds, rounds * Mult) == AbsorbRef(rounds, rounds * Mult);
        Check("block stored and loaded", ok && st[8] == 0);

        ok = true;
        for (int seed = 0; seed < 10; seed++)
            ok &= Mixed(p, 50 + seed, seed) == MixedRef(len, 50 + seed, seed);
        Check("mixed with calls", ok);

        ok = true;
        for (int seed = 0; seed < 20; seed++)
        {
            ok &= TryProbe(p, -1, seed) == TryProbeRef(-1, seed);
            ok &= TryProbe(p, seed + 2, seed) == TryProbeRef(seed + 2, seed);
        }
        Check("try, finally, catch", ok);

        ok = true;
        for (int depth = 0; depth < 8; depth++)
        {
            ok &= CatchKeep(depth, -1, depth, p);
            ok &= CatchKeep(depth, 50 + depth, depth, p);
        }
        Check("throw through probes", ok);

        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails;
    }
}
