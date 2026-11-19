// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Zero initialization of locals in the prolog and by `= default`.
//
// A method whose locals must start as zero (a struct that is passed by
// address before it is assigned, object references the GC may look at)
// clears them in its prolog: single stores for a few words, a loop for more,
// and on ZisK (JitZkMemsetCsr) one memset precompile instruction pair for
// the whole range, with the length in a scratch register. A struct local
// that is assigned `default` is cleared the same way in the body. What can
// go wrong: a range that starts or ends at the wrong offset, so that a local
// keeps what an earlier call left on the stack or a neighbour is cleared, a
// length or an offset that does not fit an immediate, a scratch register
// that holds an incoming argument, and a local that is cleared only once
// when the method clears it in a loop.
//
// Each method runs for locals of 1 byte to 4104 bytes, with sizes on both
// sides of every limit (24, 96, 256, 2048, 4096). The stack below the caller
// is filled with a non-zero pattern before every call, so a byte that is not
// cleared does not read as zero by luck. The methods check that every byte
// of the local is zero, that their arguments (eight in registers, more on
// the stack) and the values around the local are intact, and that locals
// with object references read as null.
//
// The locals that are read before they are assigned rely on `.locals init`.
// A platform that turns it off (JitZkInitLocallocOnly=1, the ZisK default)
// leaves them as they are, so there the test has to be built with
// JitZkInitLocallocOnly=0.
//
// The checks are loops over single bytes compiled without optimization. Each
// group prints one line; the run ends with ALL PASS or a FAIL count.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

unsafe struct Z1 { public fixed byte B[1]; }
unsafe struct Z3 { public fixed byte B[3]; }
unsafe struct Z4 { public fixed byte B[4]; }
unsafe struct Z7 { public fixed byte B[7]; }
unsafe struct Z8 { public fixed byte B[8]; }
unsafe struct Z12 { public fixed byte B[12]; }
unsafe struct Z15 { public fixed byte B[15]; }
unsafe struct Z16 { public fixed byte B[16]; }
unsafe struct Z20 { public fixed byte B[20]; }
unsafe struct Z23 { public fixed byte B[23]; }
unsafe struct Z24 { public fixed byte B[24]; }
unsafe struct Z25 { public fixed byte B[25]; }
unsafe struct Z31 { public fixed byte B[31]; }
unsafe struct Z32 { public fixed byte B[32]; }
unsafe struct Z33 { public fixed byte B[33]; }
unsafe struct Z36 { public fixed byte B[36]; }
unsafe struct Z40 { public fixed byte B[40]; }
unsafe struct Z47 { public fixed byte B[47]; }
unsafe struct Z48 { public fixed byte B[48]; }
unsafe struct Z56 { public fixed byte B[56]; }
unsafe struct Z64 { public fixed byte B[64]; }
unsafe struct Z72 { public fixed byte B[72]; }
unsafe struct Z80 { public fixed byte B[80]; }
unsafe struct Z88 { public fixed byte B[88]; }
unsafe struct Z95 { public fixed byte B[95]; }
unsafe struct Z96 { public fixed byte B[96]; }
unsafe struct Z97 { public fixed byte B[97]; }
unsafe struct Z104 { public fixed byte B[104]; }
unsafe struct Z112 { public fixed byte B[112]; }
unsafe struct Z120 { public fixed byte B[120]; }
unsafe struct Z128 { public fixed byte B[128]; }
unsafe struct Z136 { public fixed byte B[136]; }
unsafe struct Z200 { public fixed byte B[200]; }
unsafe struct Z255 { public fixed byte B[255]; }
unsafe struct Z256 { public fixed byte B[256]; }
unsafe struct Z257 { public fixed byte B[257]; }
unsafe struct Z512 { public fixed byte B[512]; }
unsafe struct Z1000 { public fixed byte B[1000]; }
unsafe struct Z2024 { public fixed byte B[2024]; }
unsafe struct Z2032 { public fixed byte B[2032]; }
unsafe struct Z2040 { public fixed byte B[2040]; }
unsafe struct Z2047 { public fixed byte B[2047]; }
unsafe struct Z2048 { public fixed byte B[2048]; }
unsafe struct Z2056 { public fixed byte B[2056]; }
unsafe struct Z2064 { public fixed byte B[2064]; }
unsafe struct Z3000 { public fixed byte B[3000]; }
unsafe struct Z4088 { public fixed byte B[4088]; }
unsafe struct Z4096 { public fixed byte B[4096]; }
unsafe struct Z4104 { public fixed byte B[4104]; }

struct G6 { public long L0; public object O0; public long L1; public string S0; public int[] A0; public long L2; }

static unsafe class Ref
{
    const MethodImplOptions Plain = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;

    // The number of bytes that are not zero.
    [MethodImpl(Plain)]
    public static int NonZero(void* p, int n)
    {
        int count = 0;
        for (int i = 0; i < n; i++)
        {
            if (((byte*)p)[i] != 0)
                count++;
        }
        return count;
    }

    [MethodImpl(Plain)]
    public static void Scribble(void* p, int n, int seed)
    {
        for (int i = 0; i < n; i++)
            ((byte*)p)[i] = (byte)(0x80 | (seed + i));
    }

    [MethodImpl(Plain)]
    public static void Touch(void* p)
    {
    }

    // Leaves a non-zero pattern in the stack below the caller.
    [MethodImpl(Plain)]
    public static int Dirty(int bytes)
    {
        byte* p = stackalloc byte[bytes];
        for (int i = 0; i < bytes; i++)
            p[i] = (byte)(0xC1 + i * 3 | 1);
        return p[bytes - 1];
    }

    [MethodImpl(Plain)]
    public static long IsClear(ref G6 g) =>
        ((g.L0 == 0) ? 0 : 1) + ((g.O0 == null) ? 0 : 2) + ((g.L1 == 0) ? 0 : 4) + ((g.S0 == null) ? 0 : 8) +
        ((g.A0 == null) ? 0 : 16) + ((g.L2 == 0) ? 0 : 32);
}

// The methods under test, one instantiation per size of the local.
static unsafe class Frames<T> where T : unmanaged
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long One(long x)
    {
        Unsafe.SkipInit(out T s);
        Ref.Touch(&s);
        return Ref.NonZero(&s, sizeof(T)) * 1000003L + x;
    }

    // Eight arguments in registers and two on the stack, all used after the prolog.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Args(long a, long b, long c, long d, long e, long f, long g, long h, long i, long j)
    {
        Unsafe.SkipInit(out T s);
        Ref.Touch(&s);
        return Ref.NonZero(&s, sizeof(T)) * 1000003L + a + b * 3 + c * 5 + d * 7 + e * 11 + f * 13 + g * 17 +
               h * 19 + i * 23 + j * 29;
    }

    // Two locals to clear, an initialized one between them and object references.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Mixed(long a, object o, long b)
    {
        Unsafe.SkipInit(out T s);
        long mid = a * 3 + b;
        Unsafe.SkipInit(out T t);
        Unsafe.SkipInit(out G6 g);
        Ref.Touch(&s);
        Ref.Touch(&mid);
        Ref.Touch(&t);
        Ref.Touch(&g.L0);
        long clear = Ref.IsClear(ref g);
        g.O0 = o;
        g.L2 = b;
        return (Ref.NonZero(&s, sizeof(T)) + Ref.NonZero(&t, sizeof(T))) * 1000003L + clear * 1009 + mid +
               (ReferenceEquals(g.O0, o) ? 7 : 0) + g.L2;
    }

    // A large initialized local first: the cleared range lies far from the frame's base.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Far(long x)
    {
        Z3000 big;
        Ref.Scribble(&big, sizeof(Z3000), (int)x);
        Unsafe.SkipInit(out T s);
        Ref.Touch(&s);
        long nonZero = Ref.NonZero(&s, sizeof(T));
        return nonZero * 1000003L + (sizeof(Z3000) - Ref.NonZero(&big, sizeof(Z3000))) * 1009L + x;
    }

    // Protected region: the locals live on the frame, the handler reads them.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Handler(long x, int[] arr)
    {
        Unsafe.SkipInit(out T s);
        long result = 0;
        try
        {
            Ref.Touch(&s);
            result = Ref.NonZero(&s, sizeof(T)) * 1000003L + arr[(int)x];
        }
        catch (IndexOutOfRangeException)
        {
            result = Ref.NonZero(&s, sizeof(T)) * 1000003L + x * 31;
        }
        finally
        {
            result += Ref.NonZero(&s, sizeof(T)) * 7919L;
        }
        return result;
    }

    // `default` in the body, once and in a loop that dirties the local in between.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Default(long x)
    {
        T s;
        Ref.Scribble(&s, sizeof(T), (int)x);
        long before = Ref.NonZero(&s, sizeof(T));
        s = default;
        Ref.Touch(&s);
        return Ref.NonZero(&s, sizeof(T)) * 1000003L + (sizeof(T) - before) * 1009L + x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long DefaultLoop(long x, int count)
    {
        long sum = x;
        for (int i = 0; i < count; i++)
        {
            T s = default;
            Ref.Touch(&s);
            sum += Ref.NonZero(&s, sizeof(T)) * 1000003L + i;
            Ref.Scribble(&s, sizeof(T), i);
        }
        return sum;
    }

    // Guards on both sides of a local cleared in the body.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Guards(long x)
    {
        long before = x * 3 + 1;
        T s;
        long after = x * 5 + 2;
        Ref.Touch(&before);
        Ref.Touch(&after);
        Ref.Scribble(&s, sizeof(T), (int)x);
        s = default;
        Ref.Touch(&s);
        return Ref.NonZero(&s, sizeof(T)) * 1000003L + before * 7 + after * 11;
    }

    // `new T()` and a default argument value.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long NewAndPass(long x)
    {
        T s = new T();
        return Take(s, x) + Take(default, x + 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Take(T s, long x) => Ref.NonZero(&s, sizeof(T)) * 1000003L + x;
}

static unsafe class Program
{
    static int s_fails;
    static int s_groupFails;
    static readonly object s_obj = new object();
    static readonly int[] s_arr = { 10, 20, 30 };

    static void EndGroup(string name)
    {
        if (s_groupFails == 0)
            Console.WriteLine("ok   " + name);
        else
            Console.WriteLine("FAIL " + name + " cases=" + s_groupFails);
        s_fails += s_groupFails;
        s_groupFails = 0;
    }

    static void Check(string name, int size, long expected, long got)
    {
        if (expected == got)
            return;
        s_groupFails++;
        if (s_groupFails <= 8)
            Console.WriteLine("FAIL " + name + " size " + size + " expected " + expected + " got " + got);
    }

    static void Run<T>() where T : unmanaged
    {
        int n = sizeof(T);
        int depth = 2 * n + 3200 + 512;
        for (int round = 1; round <= 3; round++)
        {
            long x = round * 100 + n;

            Ref.Dirty(depth);
            Check("One", n, x, Frames<T>.One(x));

            Ref.Dirty(depth);
            Check("Args", n, 1 + 2 * 3 + 3 * 5 + 4 * 7 + 5 * 11 + 6 * 13 + 7 * 17 + 8 * 19 + x * 23 + 10 * 29,
                  Frames<T>.Args(1, 2, 3, 4, 5, 6, 7, 8, x, 10));

            Ref.Dirty(depth);
            Check("Mixed", n, (x * 3 + 5) + 7 + 5, Frames<T>.Mixed(x, s_obj, 5));

            Ref.Dirty(depth);
            Check("Far", n, x, Frames<T>.Far(x));

            Ref.Dirty(depth);
            Check("Handler", n, 30, Frames<T>.Handler(2, s_arr));
            Ref.Dirty(depth);
            Check("Handler caught", n, x * 31, Frames<T>.Handler(x, s_arr));

            Ref.Dirty(depth);
            Check("Default", n, x, Frames<T>.Default(x));
            Check("DefaultLoop", n, x + 0 + 1 + 2 + 3, Frames<T>.DefaultLoop(x, 4));
            Check("Guards", n, (x * 3 + 1) * 7 + (x * 5 + 2) * 11, Frames<T>.Guards(x));

            Ref.Dirty(depth);
            Check("NewAndPass", n, x + x + 1, Frames<T>.NewAndPass(x));
        }
    }

    // ---- locals with object references ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long RefLocals(long a, long b, long c, long d, long e, long f, long g, long h)
    {
        Unsafe.SkipInit(out G6 g0);
        Unsafe.SkipInit(out G6 g1);
        Unsafe.SkipInit(out G6 g2);
        Unsafe.SkipInit(out G6 g3);
        Unsafe.SkipInit(out G6 g4);
        Ref.Touch(&g0.L0);
        Ref.Touch(&g1.L0);
        Ref.Touch(&g2.L0);
        Ref.Touch(&g3.L0);
        Ref.Touch(&g4.L0);
        long clear = Ref.IsClear(ref g0) + Ref.IsClear(ref g1) + Ref.IsClear(ref g2) + Ref.IsClear(ref g3) +
                     Ref.IsClear(ref g4);
        return clear * 1000003L + a + b * 3 + c * 5 + d * 7 + e * 11 + f * 13 + g * 17 + h * 19;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long RefLocalsLoop(int count, object o)
    {
        long sum = 0;
        for (int i = 0; i < count; i++)
        {
            G6 g = default;
            Ref.Touch(&g.L0);
            sum += Ref.IsClear(ref g) * 1000003L + i;
            g.O0 = o;
            g.S0 = "s";
            g.L0 = -1;
            g.L1 = -1;
            g.L2 = -1;
            g.A0 = s_arr;
        }
        return sum;
    }

    static int Main()
    {
        Run<Z1>(); Run<Z3>(); Run<Z4>(); Run<Z7>(); Run<Z8>(); Run<Z12>(); Run<Z15>(); Run<Z16>();
        Run<Z20>(); Run<Z23>(); Run<Z24>(); Run<Z25>(); Run<Z31>(); Run<Z32>(); Run<Z33>();
        Run<Z36>(); Run<Z40>(); Run<Z47>(); Run<Z48>(); Run<Z56>(); Run<Z64>(); Run<Z72>();
        Run<Z80>(); Run<Z88>(); Run<Z95>(); Run<Z96>(); Run<Z97>(); Run<Z104>(); Run<Z112>();
        Run<Z120>(); Run<Z128>(); Run<Z136>(); Run<Z200>(); Run<Z255>(); Run<Z256>(); Run<Z257>();
        Run<Z512>(); Run<Z1000>(); Run<Z2024>(); Run<Z2032>(); Run<Z2040>(); Run<Z2047>();
        Run<Z2048>(); Run<Z2056>(); Run<Z2064>(); Run<Z3000>(); Run<Z4088>(); Run<Z4096>();
        Run<Z4104>();
        EndGroup("locals of every size cleared in the prolog and by default");

        for (int round = 1; round <= 5; round++)
        {
            Ref.Dirty(2000);
            Check("RefLocals", 240, 1 + 2 * 3 + 3 * 5 + 4 * 7 + 5 * 11 + 6 * 13 + 7 * 17 + round * 19,
                  RefLocals(1, 2, 3, 4, 5, 6, 7, round));
            Ref.Dirty(2000);
            Check("RefLocalsLoop", 48, 0 + 1 + 2 + 3 + 4 + 5, RefLocalsLoop(6, s_obj));
        }
        EndGroup("locals with object references");

        Console.WriteLine(s_fails == 0 ? "ALL PASS" : "FAILED " + s_fails);
        return s_fails == 0 ? 0 : 1;
    }
}
