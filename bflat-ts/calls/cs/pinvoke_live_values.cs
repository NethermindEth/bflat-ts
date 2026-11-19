// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Values that stay live across P/Invokes, and exceptions that cross the
// frames of methods with P/Invokes.
//
// On RISC-V a P/Invoke is a plain call: there is no transition to
// preemptive mode around it, and a method with a P/Invoke gets the ordinary
// prolog. It saves only the callee-saved registers it changes and it has no
// frame pointer unless something else needs one. Two things can go wrong:
// a register that is used but not saved (the caller loses a value), and
// unwind info that does not match the smaller frame (an exception that
// crosses the frame restores the wrong registers).
//
// So the methods here keep many values live across calls to libc, more than
// there are callee-saved registers, and their callers do the same. The
// frames an exception crosses contain P/Invokes and are small, large (the
// frame is allocated in two steps above 2040 bytes) and recursive; the
// catching frame keeps its own values in registers across the try. P/Invokes
// also run inside try, catch and finally, in loops, with object references
// and a byref live across them, through qsort with a managed comparer that
// throws and catches, and through an unmanaged function pointer with
// arguments on the stack.
//
// Every result is compared with one computed without P/Invokes by code
// that is compiled without optimization. Each check prints one line; the
// run ends with ALL PASS or a FAIL count, and the guest returns the number
// of failures.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class E : Exception { public int Code; public E(int c) : base("e") { Code = c; } }

class Node { public long Val; public Node Next; public int[] Arr; }

[StructLayout(LayoutKind.Sequential, Size = 16)] struct B16 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 2032)] struct B2032 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 2048)] struct B2048 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 4104)] struct B4104 { public long First; }

// The buffer of a frame, as words. Taking its address keeps it on the stack.
static class Buf
{
    const long Mult = unchecked((long)0x9E3779B97F4A7C15UL);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Fill<T>(ref T b, int seed) where T : struct
    {
        Span<long> s = MemoryMarshal.CreateSpan(ref Unsafe.As<T, long>(ref b), Unsafe.SizeOf<T>() / 8);
        long sum = 0;
        for (int i = 0; i < s.Length; i++)
        {
            long v = (seed + 1) * Mult + i * 0x100000001B3L;
            s[i] = v;
            sum = sum * 33 + v;
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Sum<T>(ref T b) where T : struct
    {
        Span<long> s = MemoryMarshal.CreateSpan(ref Unsafe.As<T, long>(ref b), Unsafe.SizeOf<T>() / 8);
        long sum = 0;
        for (int i = 0; i < s.Length; i++)
            sum = sum * 33 + s[i];
        return sum;
    }
}

unsafe class Program
{
    [DllImport("*")] static extern nuint strlen(byte* s);
    [DllImport("*")] static extern int memcmp(void* a, void* b, nuint n);
    [DllImport("*")] static extern void* memcpy(void* d, void* s, nuint n);
    [DllImport("*")] static extern int abs(int x);
    [DllImport("*")] static extern void qsort(void* b, nuint n, nuint size, delegate* unmanaged<void*, void*, int> cmp);

    const MethodImplOptions NoOpt = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;
    const long Mult = unchecked((long)0x9E3779B97F4A7C15UL);

    static int fails;

    static void Check(string name, bool ok)
    {
        Console.WriteLine((ok ? "ok   " : "FAIL ") + name);
        if (!ok) fails++;
    }

    // A value the compiler cannot fold, and the reference for a set of them.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Mix(int seed, int i) => (seed + 1) * Mult + i * 0x100000001B3L;

    [MethodImpl(NoOpt)]
    static long Fold(int seed, int n)
    {
        long x = 0;
        for (int i = 0; i < n; i++) x ^= Mix(seed, i) * (2 * i + 1);
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Thrower(int code)
    {
        if (code >= 0) throw new E(code);
        return 0;
    }

    static int Sgn(int x) => x < 0 ? -1 : x > 0 ? 1 : 0;

    // What libc returns, computed here.
    [MethodImpl(NoOpt)]
    static int RefAbs(int x) => x < 0 ? unchecked(-x) : x;

    [MethodImpl(NoOpt)]
    static int RefCmp(byte* a, byte* b, int n)
    {
        for (int i = 0; i < n; i++)
            if (a[i] != b[i]) return a[i] < b[i] ? -1 : 1;
        return 0;
    }

    // ---- many values live across P/Invokes ----

    // Twelve values across four calls: more than the callee-saved registers.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Live12(int seed, byte* p)
    {
        long v0 = Mix(seed, 0), v1 = Mix(seed, 1), v2 = Mix(seed, 2), v3 = Mix(seed, 3);
        long v4 = Mix(seed, 4), v5 = Mix(seed, 5), v6 = Mix(seed, 6), v7 = Mix(seed, 7);
        long v8 = Mix(seed, 8), v9 = Mix(seed, 9), v10 = Mix(seed, 10), v11 = Mix(seed, 11);
        long r = (long)strlen(p);
        r = r * 31 + abs((int)v0);
        r = r * 31 + Sgn(memcmp(p, p + 1, 4));
        r = r * 31 + (long)strlen(p + 5);
        return r * 1000003 + (v0 ^ v1 * 3 ^ v2 * 5 ^ v3 * 7 ^ v4 * 9 ^ v5 * 11 ^ v6 * 13 ^ v7 * 15
                              ^ v8 * 17 ^ v9 * 19 ^ v10 * 21 ^ v11 * 23);
    }

    [MethodImpl(NoOpt)]
    static long Live12Ref(int seed, byte* p, int len)
    {
        long r = len;
        r = r * 31 + RefAbs((int)Mix(seed, 0));
        r = r * 31 + RefCmp(p, p + 1, 4);
        r = r * 31 + (len - 5);
        return r * 1000003 + Fold(seed, 12);
    }

    // Wrappers that are nothing but the call, and one with a value across it.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Len(byte* p) => (long)strlen(p);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long LenPlus(byte* p, long a, long b)
    {
        long t = a * b;
        return (long)strlen(p) + t;
    }

    // The caller of methods with P/Invokes keeps eleven values of its own.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Outer(int seed, byte* p)
    {
        long k0 = Mix(seed + 50, 0), k1 = Mix(seed + 50, 1), k2 = Mix(seed + 50, 2), k3 = Mix(seed + 50, 3);
        long k4 = Mix(seed + 50, 4), k5 = Mix(seed + 50, 5), k6 = Mix(seed + 50, 6), k7 = Mix(seed + 50, 7);
        long k8 = Mix(seed + 50, 8), k9 = Mix(seed + 50, 9), k10 = Mix(seed + 50, 10);
        long r = Live12(seed, p);
        r += Len(p + 2);
        r += LenPlus(p + 4, k0, k1);
        return r ^ (k0 ^ k1 * 3 ^ k2 * 5 ^ k3 * 7 ^ k4 * 9 ^ k5 * 11 ^ k6 * 13 ^ k7 * 15
                    ^ k8 * 17 ^ k9 * 19 ^ k10 * 21);
    }

    [MethodImpl(NoOpt)]
    static long OuterRef(int seed, byte* p, int len)
    {
        long r = Live12Ref(seed, p, len);
        r += len - 2;
        r += (len - 4) + Mix(seed + 50, 0) * Mix(seed + 50, 1);
        return r ^ Fold(seed + 50, 11);
    }

    static void Live(byte* p, int len)
    {
        bool ok = true, okOuter = true;
        for (int seed = 0; seed < 40; seed++)
        {
            ok &= Live12(seed, p) == Live12Ref(seed, p, len);
            okOuter &= Outer(seed, p) == OuterRef(seed, p, len);
        }
        Check("live values", ok);
        Check("caller values", okOuter);
    }

    // ---- exceptions through frames with P/Invokes ----

    // Ten values in registers, no frame pointer.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long MidRegs(int depth, int code, int seed, byte* p)
    {
        long v0 = Mix(seed, 0), v1 = Mix(seed, 1), v2 = Mix(seed, 2), v3 = Mix(seed, 3), v4 = Mix(seed, 4);
        long v5 = Mix(seed, 5), v6 = Mix(seed, 6), v7 = Mix(seed, 7), v8 = Mix(seed, 8), v9 = Mix(seed, 9);
        long r = (long)strlen(p + (depth & 3));
        r += depth == 0 ? Thrower(code) : MidRegs(depth - 1, code, seed + 1, p);
        r += abs(code);
        return r + (v0 ^ v1 * 3 ^ v2 * 5 ^ v3 * 7 ^ v4 * 9 ^ v5 * 11 ^ v6 * 13 ^ v7 * 15 ^ v8 * 17 ^ v9 * 19);
    }

    // A buffer of the size under test on the frame, handed to libc.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long MidBig<T>(int depth, int code, int seed, byte* p) where T : unmanaged
    {
        T buf = default;
        long want = Buf.Fill(ref buf, seed);
        long v0 = Mix(seed, 0), v1 = Mix(seed, 1), v2 = Mix(seed, 2), v3 = Mix(seed, 3);
        long r = (long)strlen(p + (depth & 3));
        r += memcmp(&buf, &buf, (nuint)sizeof(T));
        r += depth == 0 ? Thrower(code) : MidBig<T>(depth - 1, code, seed + 1, p);
        r += abs(code);
        if (Buf.Sum(ref buf) != want) r += 1 << 20;
        return r + (v0 ^ v1 * 3 ^ v2 * 5 ^ v3 * 7);
    }

    // The result of either when nothing is thrown (code is -1).
    [MethodImpl(NoOpt)]
    static long MidRef(int depth, int seed, int len, int values)
    {
        long r = len - (depth & 3);
        if (depth != 0) r += MidRef(depth - 1, seed + 1, len, values);
        r += 1;
        return r + Fold(seed, values);
    }

    // The catching frame keeps eight values across the try.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool CatchKeep(int kind, int depth, int code, int seed, byte* p, int len)
    {
        long k0 = Mix(seed + 100, 0), k1 = Mix(seed + 100, 1), k2 = Mix(seed + 100, 2), k3 = Mix(seed + 100, 3);
        long k4 = Mix(seed + 100, 4), k5 = Mix(seed + 100, 5), k6 = Mix(seed + 100, 6), k7 = Mix(seed + 100, 7);
        int got = -1;
        long r = 0;
        try
        {
            switch (kind)
            {
                case 0: r = MidRegs(depth, code, seed, p); break;
                case 1: r = MidBig<B16>(depth, code, seed, p); break;
                case 2: r = MidBig<B2032>(depth, code, seed, p); break;
                case 3: r = MidBig<B2048>(depth, code, seed, p); break;
                default: r = MidBig<B4104>(depth, code, seed, p); break;
            }
        }
        catch (E e)
        {
            got = e.Code;
        }
        if ((k0 ^ k1 * 3 ^ k2 * 5 ^ k3 * 7 ^ k4 * 9 ^ k5 * 11 ^ k6 * 13 ^ k7 * 15) != Fold(seed + 100, 8))
            return false;
        if (code >= 0) return got == code;
        return got == -1 && r == MidRef(depth, seed, len, kind == 0 ? 10 : 4);
    }

    static readonly string[] kindNames = { "registers", "frame 16", "frame 2032", "frame 2048", "frame 4104" };

    static void Throws(byte* p, int len)
    {
        for (int kind = 0; kind < 5; kind++)
        {
            bool ok = true;
            for (int depth = 0; depth < 5; depth++)
            {
                ok &= CatchKeep(kind, depth, -1, depth * 7 + kind, p, len);
                ok &= CatchKeep(kind, depth, 40 + depth, depth * 7 + kind, p, len);
            }
            Check("throw through " + kindNames[kind], ok);
        }
    }

    // ---- P/Invokes inside try, finally and catch ----

    static int finRuns;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long TryCalls(int seed, int code, byte* p)
    {
        long v0 = Mix(seed, 0), v1 = Mix(seed, 1), v2 = Mix(seed, 2);
        long v3 = Mix(seed, 3), v4 = Mix(seed, 4), v5 = Mix(seed, 5);
        long r = 0;
        try
        {
            r += (long)strlen(p);
            try
            {
                r += abs(seed - 1000);
                Thrower(code);
                r += 7;
            }
            finally
            {
                r = r * 3 + Sgn(memcmp(p, p + 2, 8)) + (long)strlen(p + 1);
                finRuns++;
            }
        }
        catch (E e)
        {
            r = r * 5 + abs(-e.Code) + (long)strlen(p + 2);
        }
        r += (long)strlen(p + 3);
        return r * 1000003 + (v0 ^ v1 * 3 ^ v2 * 5 ^ v3 * 7 ^ v4 * 9 ^ v5 * 11);
    }

    [MethodImpl(NoOpt)]
    static long TryCallsRef(int seed, int code, byte* p, int len)
    {
        long r = len;
        r += RefAbs(seed - 1000);
        if (code < 0) r += 7;
        r = r * 3 + RefCmp(p, p + 2, 8) + (len - 1);
        if (code >= 0) r = r * 5 + code + (len - 2);
        r += len - 3;
        return r * 1000003 + Fold(seed, 6);
    }

    static void TryBlocks(byte* p, int len)
    {
        bool ok = true;
        finRuns = 0;
        for (int seed = 0; seed < 20; seed++)
        {
            ok &= TryCalls(seed, -1, p) == TryCallsRef(seed, -1, p, len);
            ok &= TryCalls(seed, seed + 3, p) == TryCallsRef(seed, seed + 3, p, len);
        }
        Check("calls in try, finally, catch", ok && finRuns == 40);
    }

    // ---- object references and a byref across P/Invokes ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Refs(int seed, byte* p)
    {
        int[] arr = new int[8];
        for (int i = 0; i < arr.Length; i++) arr[i] = seed + i;
        Node n = new Node { Val = Mix(seed, 1), Arr = arr };
        n.Next = new Node { Val = Mix(seed, 2) };
        object boxed = Mix(seed, 3);
        string s = new string('x', (seed & 7) + 1);
        ref int slot = ref arr[3];
        long r = (long)strlen(p);
        byte[] big = new byte[64 + (seed & 7)];
        slot += 5;
        fixed (byte* q = big)
        {
            memcpy(q, p, 16);
            r = r * 31 + Sgn(memcmp(q, p, 16));
            r = r * 31 + (long)strlen(q);
        }
        r = r * 31 + abs(-seed);
        long sum = 0;
        for (int i = 0; i < arr.Length; i++) sum += arr[i];
        return r + sum * 7 + n.Val + n.Next.Val * 3 + (long)boxed * 5 + s.Length * 11 + n.Arr[3] * 13
               + big[15] * 17 + big.Length;
    }

    [MethodImpl(NoOpt)]
    static long RefsRef(int seed, byte* p, int len)
    {
        long r = len;
        r = r * 31;
        r = r * 31 + 16;
        r = r * 31 + RefAbs(-seed);
        long sum = 8 * seed + 28 + 5;
        return r + sum * 7 + Mix(seed, 1) + Mix(seed, 2) * 3 + Mix(seed, 3) * 5 + ((seed & 7) + 1) * 11
               + (seed + 8) * 13 + p[15] * 17 + 64 + (seed & 7);
    }

    static void References(byte* p, int len)
    {
        bool ok = true;
        for (int seed = 0; seed < 30; seed++)
            ok &= Refs(seed, p) == RefsRef(seed, p, len);
        Check("references", ok);
    }

    // ---- P/Invokes in a loop ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Loop(int n, byte* p)
    {
        long acc = 0, x = 1;
        int neg = 0;
        for (int i = 0; i < n; i++)
        {
            acc += (long)strlen(p + (i & 31));
            x = x * 6364136223846793005L + 1442695040888963407L;
            neg += abs((int)(x >> 40));
            if ((i & 7) == 0) acc ^= (long)(Sgn(memcmp(p + (i & 15), p + 3, 4)) + 2) << (i & 31);
        }
        return acc + x * 3 + neg;
    }

    [MethodImpl(NoOpt)]
    static long LoopRef(int n, byte* p, int len)
    {
        long acc = 0, x = 1;
        int neg = 0;
        for (int i = 0; i < n; i++)
        {
            acc += len - (i & 31);
            x = x * 6364136223846793005L + 1442695040888963407L;
            neg += RefAbs((int)(x >> 40));
            if ((i & 7) == 0) acc ^= (long)(RefCmp(p + (i & 15), p + 3, 4) + 2) << (i & 31);
        }
        return acc + x * 3 + neg;
    }

    // ---- a managed comparer called back by qsort ----

    static int cmpCalls, cmpCaught;

    [UnmanagedCallersOnly]
    static int CmpInt(void* a, void* b)
    {
        cmpCalls++;
        int x = *(int*)a, y = *(int*)b;
        return x < y ? -1 : x > y ? 1 : 0;
    }

    // Throws and catches under the frames of qsort.
    [UnmanagedCallersOnly]
    static int CmpCatch(void* a, void* b)
    {
        int x = *(int*)a, y = *(int*)b;
        try
        {
            if (((x ^ y) & (1 << 20)) != 0) Thrower(x & 0xff);
        }
        catch (E)
        {
            cmpCaught++;
        }
        cmpCalls++;
        return x < y ? -1 : x > y ? 1 : 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Sort(int seed, int* data, int n, delegate* unmanaged<void*, void*, int> cmp)
    {
        long v0 = Mix(seed, 0), v1 = Mix(seed, 1), v2 = Mix(seed, 2), v3 = Mix(seed, 3);
        long v4 = Mix(seed, 4), v5 = Mix(seed, 5), v6 = Mix(seed, 6), v7 = Mix(seed, 7);
        long sum = 0;
        for (int i = 0; i < n; i++)
        {
            data[i] = (int)(Mix(seed, i) >> 20);
            sum += data[i];
        }
        qsort(data, (nuint)n, 4, cmp);
        long bad = 0;
        for (int i = 0; i < n; i++)
        {
            sum -= data[i];
            if (i > 0 && data[i - 1] > data[i]) bad++;
        }
        return bad * 1000 + sum + (v0 ^ v1 * 3 ^ v2 * 5 ^ v3 * 7 ^ v4 * 9 ^ v5 * 11 ^ v6 * 13 ^ v7 * 15);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool SortThenThrow(int seed, int* data, int n, delegate* unmanaged<void*, void*, int> cmp)
    {
        long k0 = Mix(seed + 9, 0), k1 = Mix(seed + 9, 1), k2 = Mix(seed + 9, 2), k3 = Mix(seed + 9, 3);
        int got = -1;
        long r = 0;
        try
        {
            r = Sort(seed, data, n, cmp);
            Thrower(77);
        }
        catch (E e)
        {
            got = e.Code;
        }
        return got == 77 && r == Fold(seed, 8) && (k0 ^ k1 * 3 ^ k2 * 5 ^ k3 * 7) == Fold(seed + 9, 4);
    }

    static void Callbacks(int* data)
    {
        bool ok = true;
        cmpCalls = 0;
        for (int seed = 0; seed < 10; seed++)
            ok &= Sort(seed, data, 8 + seed * 2, &CmpInt) == Fold(seed, 8);
        Check("qsort comparer", ok && cmpCalls > 0);

        ok = true;
        cmpCalls = 0; cmpCaught = 0;
        for (int seed = 0; seed < 4; seed++)
            ok &= Sort(seed, data, 12 + seed * 4, &CmpCatch) == Fold(seed, 8);
        Check("qsort comparer that throws and catches", ok && cmpCaught > 0 && cmpCaught < cmpCalls);

        ok = true;
        for (int seed = 0; seed < 4; seed++)
            ok &= SortThenThrow(seed, data, 16, (seed & 1) == 0 ? &CmpInt : &CmpCatch);
        Check("throw after qsort", ok);
    }

    // ---- an unmanaged function pointer with arguments on the stack ----

    [UnmanagedCallersOnly]
    static long Uco12(long a0, long a1, long a2, long a3, long a4, long a5,
                      long a6, long a7, long a8, long a9, long a10, long a11)
    {
        return a0 ^ a1 * 3 ^ a2 * 5 ^ a3 * 7 ^ a4 * 9 ^ a5 * 11 ^ a6 * 13 ^ a7 * 15
               ^ a8 * 17 ^ a9 * 19 ^ a10 * 21 ^ a11 * 23;
    }

    // Kept in a field so that the call stays indirect.
    static delegate* unmanaged<long, long, long, long, long, long, long, long, long, long, long, long, long> uco12;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long CallUco12(int seed)
    {
        long v0 = Mix(seed, 0), v1 = Mix(seed, 1), v2 = Mix(seed, 2), v3 = Mix(seed, 3);
        long v4 = Mix(seed, 4), v5 = Mix(seed, 5), v6 = Mix(seed, 6), v7 = Mix(seed, 7);
        long v8 = Mix(seed, 8), v9 = Mix(seed, 9), v10 = Mix(seed, 10), v11 = Mix(seed, 11);
        long r = uco12(v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11);
        r += uco12(v11, v10, v9, v8, v7, v6, v5, v4, v3, v2, v1, v0);
        return r + (v0 ^ v11) + (v5 ^ v6);
    }

    [MethodImpl(NoOpt)]
    static long CallUco12Ref(int seed)
    {
        long back = 0;
        for (int i = 0; i < 12; i++) back ^= Mix(seed, 11 - i) * (2 * i + 1);
        return Fold(seed, 12) + back + (Mix(seed, 0) ^ Mix(seed, 11)) + (Mix(seed, 5) ^ Mix(seed, 6));
    }

    static void Indirect()
    {
        uco12 = &Uco12;
        bool ok = true;
        for (int seed = 0; seed < 30; seed++)
            ok &= CallUco12(seed) == CallUco12Ref(seed);
        Check("unmanaged function pointer", ok);
    }

    static int Main()
    {
        const int len = 47;
        byte* p = stackalloc byte[64];
        for (int i = 0; i < len; i++) p[i] = (byte)('a' + i * 7 % 23);
        p[len] = 0;
        int* data = stackalloc int[32];

        Live(p, len);
        Throws(p, len);
        TryBlocks(p, len);
        References(p, len);
        Check("loop", Loop(2000, p) == LoopRef(2000, p, len));
        Callbacks(data);
        Indirect();

        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails;
    }
}
