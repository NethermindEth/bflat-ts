// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Exceptions that unwind through large stack frames.
//
// A RISC-V method whose frame does not fit in a 12-bit immediate allocates it
// in two steps. A method without a frame pointer keeps its CFA on SP, so both
// steps have to be in its unwind info; with only the first one there, the
// unwinder takes the return address from the wrong slot and the runtime fails
// fast (exit code 134). Only a method with no handler of its own and no
// stackalloc is compiled without a frame pointer, so the frames an exception
// crosses here have neither: it is thrown below them and caught above them.
//
// The frame sizes step over the 2040-byte limit in 8-byte steps and go up to
// 16 KiB. Every frame holds a buffer of the size under test, filled before
// the call and checked after it, so a prolog and an epilog that disagree
// show up on the path without a throw too. Frames with a finally or a catch
// have a frame pointer and are the control for the same sizes.
//
// Each size prints one line; the run ends with ALL PASS or a FAIL count, and
// the guest returns the number of failures.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class E : Exception { public int Code; public E(int c) : base("e") { Code = c; } }

[StructLayout(LayoutKind.Sequential, Size = 8)] struct B8 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 16)] struct B16 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 64)] struct B64 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 256)] struct B256 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 1024)] struct B1024 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 1536)] struct B1536 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 1904)] struct B1904 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 1920)] struct B1920 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 1936)] struct B1936 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 1952)] struct B1952 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 1968)] struct B1968 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 1976)] struct B1976 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 1984)] struct B1984 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 1992)] struct B1992 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 2000)] struct B2000 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 2008)] struct B2008 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 2016)] struct B2016 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 2024)] struct B2024 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 2032)] struct B2032 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 2040)] struct B2040 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 2048)] struct B2048 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 2056)] struct B2056 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 2064)] struct B2064 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 2072)] struct B2072 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 2080)] struct B2080 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 2096)] struct B2096 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 2128)] struct B2128 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 2304)] struct B2304 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 3072)] struct B3072 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 4080)] struct B4080 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 4088)] struct B4088 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 4096)] struct B4096 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 4104)] struct B4104 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 6144)] struct B6144 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 8192)] struct B8192 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 12288)] struct B12288 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 16384)] struct B16384 { public long First; }

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

// A generic virtual method and an interface method of a boxed struct with a
// large frame: the frames an exception crosses are entered through a
// dictionary lookup and an unboxing stub.
abstract class Walker { public abstract int Walk<T>(int depth, int code) where T : struct; }

class DeepWalker : Walker
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override int Walk<T>(int depth, int code)
    {
        T buf = default;
        long want = Buf.Fill(ref buf, depth + code);
        int r = depth == 0 ? Program.Thrower(code) : Walk<T>(depth - 1, code);
        return r + (Buf.Sum(ref buf) == want ? 0 : 1);
    }
}

interface IStep { int Run(int depth, int code); }

struct BigStep<T> : IStep where T : struct
{
    public T Pad;
    public int Seed;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int Run(int depth, int code)
    {
        T buf = Pad;
        long want = Buf.Fill(ref buf, Seed + depth);
        int r = depth == 0 ? Program.Thrower(code) : Run(depth - 1, code);
        return r + (Buf.Sum(ref buf) == want ? 0 : 1);
    }
}

delegate int Step(int i, int code);

class Program
{
    static int fails;

    static void Check(string name, bool ok)
    {
        Console.WriteLine((ok ? "ok   " : "FAIL ") + name);
        if (!ok) fails++;
    }

    // Returns 0 for a negative code, so every path can be run without a throw.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static int Thrower(int code)
    {
        if (code < 0) return 0;
        throw new E(code);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Op(long v) => v;

    // ---- frames without a handler: no frame pointer ----

    // The plain case: a buffer live across the call.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Through<T>(int depth, int code) where T : struct
    {
        T buf = default;
        long want = Buf.Fill(ref buf, depth + code);
        int r = depth == 0 ? Thrower(code) : Through<T>(depth - 1, code);
        return r + (Buf.Sum(ref buf) == want ? 0 : 1);
    }

    // Ten more values live across the call, so the callee-saved registers
    // are all in use and their save area is at its largest. The split of
    // the frame depends on the locals and on that area together.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Regs<T>(int depth, int code, long a, long b, long c, long d, long e, long f) where T : struct
    {
        T buf = default;
        long want = Buf.Fill(ref buf, depth + code);
        long x0 = a + 1, x1 = b * 3, x2 = c ^ 5, x3 = d - 7, x4 = e + a;
        long x5 = f + b, x6 = a * c, x7 = b + d, x8 = c + e, x9 = d + f;
        long r = depth == 0 ? Thrower(code) : Regs<T>(depth - 1, code, x1, x2, x3, x4, x5, x0);
        return r + x0 + x1 + x2 + x3 + x4 + x5 + x6 + x7 + x8 + x9 + (Buf.Sum(ref buf) == want ? 0 : 1);
    }

    // The same arithmetic with no buffer: what Regs returns without a throw.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long RegsRef(int depth, long a, long b, long c, long d, long e, long f)
    {
        long x0 = a + 1, x1 = b * 3, x2 = c ^ 5, x3 = d - 7, x4 = e + a;
        long x5 = f + b, x6 = a * c, x7 = b + d, x8 = c + e, x9 = d + f;
        long r = depth == 0 ? 0 : RegsRef(depth - 1, x1, x2, x3, x4, x5, x0);
        return r + x0 + x1 + x2 + x3 + x4 + x5 + x6 + x7 + x8 + x9;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Sink(long a1, long a2, long a3, long a4, long a5, long a6, long a7, long a8,
                     long a9, long a10, long a11, long a12, int code)
    {
        long s = a1 + a2 + a3 + a4 + a5 + a6 + a7 + a8 + a9 + a10 + a11 + a12;
        return s + Thrower(code);
    }

    // Arguments on the stack: the frame has an outgoing argument area.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Args<T>(int code, long k) where T : struct
    {
        T buf = default;
        long want = Buf.Fill(ref buf, code);
        long r = Sink(k, k + 1, k + 2, k + 3, k + 4, k + 5, k + 6, k + 7, k + 8, k + 9, k + 10, k + 11, code);
        return r + (Buf.Sum(ref buf) == want ? 0 : 1);
    }

    // The throw is in the large frame itself: it is the first one unwound.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ThrowHere<T>(int code) where T : struct
    {
        T buf = default;
        long want = Buf.Fill(ref buf, code);
        if (code >= 0)
            throw new E(code + (Buf.Sum(ref buf) == want ? 0 : 1000));
        return 0;
    }

    // The runtime's own throws, raised from the large frame or below it:
    // bounds check, division, cast, overflow, array store and a parse error
    // from the depths of the library.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Helper<T>(int kind, int[] arr, int zero, object o) where T : struct
    {
        T buf = default;
        long want = Buf.Fill(ref buf, kind);
        int r;
        switch (kind)
        {
            case 0: r = arr[arr.Length + zero]; break;
            case 1: r = arr.Length / zero; break;
            case 2: r = ((string)o).Length; break;
            case 3: r = checked(int.MaxValue + arr.Length + zero); break;
            case 4: ((object[])o)[zero] = arr; r = 0; break;
            case 5: r = int.Parse((string)o); break;
            default: r = arr[zero]; break;
        }
        return r + (Buf.Sum(ref buf) == want ? 0 : 1);
    }

    // A large value built in one large frame and passed by value to another.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static T Make<T>(int code) where T : struct
    {
        T b = default;
        Buf.Fill(ref b, 77);
        Thrower(code);
        return b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ByValue<T>(T v, int code) where T : struct
    {
        long want = Buf.Sum(ref v);
        int r = Thrower(code);
        return r + (Buf.Sum(ref v) == want ? 0 : 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ValueFlow<T>(int code1, int code2) where T : struct
    {
        T v = Make<T>(code1);
        T reference = default;
        long want = Buf.Fill(ref reference, 77);
        return ByValue(v, code2) + (Buf.Sum(ref v) == want ? 0 : 1);
    }

    // ---- frames with a handler: a frame pointer, the control ----

    static int finRuns, finBad, rethrows;

    // The finally runs in the second pass and reads the buffer of its frame.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Fin<T>(int depth, int code) where T : struct
    {
        T buf = default;
        long want = Buf.Fill(ref buf, depth + code);
        try
        {
            return depth == 0 ? Thrower(code) : Fin<T>(depth - 1, code);
        }
        finally
        {
            finRuns++;
            if (Buf.Sum(ref buf) != want) finBad++;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int CatchHere<T>(int depth, int code) where T : struct
    {
        T buf = default;
        long want = Buf.Fill(ref buf, depth + code);
        try
        {
            return Through<T>(depth, code);
        }
        catch (E e)
        {
            return e.Code + (Buf.Sum(ref buf) == want ? 0 : 1000);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Rethrow<T>(int depth, int code) where T : struct
    {
        T buf = default;
        long want = Buf.Fill(ref buf, depth + code);
        try
        {
            return Through<T>(depth, code);
        }
        catch (E)
        {
            if (Buf.Sum(ref buf) == want) rethrows++;
            throw;
        }
    }

    // The finally replaces the exception while large frames are unwound.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int FinThrows<T>(int depth, int code, int newCode) where T : struct
    {
        T buf = default;
        long want = Buf.Fill(ref buf, depth + code);
        try
        {
            return Through<T>(depth, code);
        }
        finally
        {
            if (Buf.Sum(ref buf) == want) Thrower(newCode);
        }
    }

    // A filter runs in the first pass, on top of the frames the exception
    // has not left yet. It calls through large frames and dispatches an
    // exception of its own through them.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Probe<T>(E e, ref int seen) where T : struct
    {
        seen += Through<T>(1, -1) == 0 ? 100 : 5000;
        try { Through<T>(1, 9); } catch (E inner) { seen += inner.Code; }
        return e.Code == 70;
    }

    static int Filter<T>() where T : struct
    {
        int seen = 0;
        try { Through<T>(2, 70); }
        catch (E e) when (Probe<T>(e, ref seen)) { return e.Code + seen; }
        return -1;
    }

    // ---- every case for one frame size ----

    static readonly int[] three = new int[3];

    static bool Raises<TEx>(Func<int> f) where TEx : Exception
    {
        try { f(); } catch (TEx) { return true; } catch (Exception) { return false; }
        return false;
    }

    static void Size<T>(string name) where T : struct
    {
        int bad = 0;
        long c0 = Op(0x1111), c1 = Op(0x2222), c2 = Op(0x3333), c3 = Op(0x4444);
        long c4 = Op(0x5555), c5 = Op(0x6666), c6 = Op(0x7777), c7 = Op(0x8888);
        int got;

        // Without a throw: the prolog and the epilog agree.
        if (Through<T>(3, -1) != 0) bad |= 1 << 0;
        if (Regs<T>(3, -1, 1, 2, 3, 4, 5, 6) != RegsRef(3, 1, 2, 3, 4, 5, 6)) bad |= 1 << 1;
        if (Args<T>(-1, 10) != 12 * 10 + 66) bad |= 1 << 2;

        // Through one frame and through several.
        got = -1; try { Through<T>(0, 41); } catch (E e) { got = e.Code; }
        if (got != 41) bad |= 1 << 3;
        got = -1; try { Through<T>(3, 42); } catch (E e) { got = e.Code; }
        if (got != 42) bad |= 1 << 4;

        got = -1; try { Regs<T>(0, 43, 1, 2, 3, 4, 5, 6); } catch (E e) { got = e.Code; }
        if (got != 43) bad |= 1 << 5;
        got = -1; try { Regs<T>(3, 44, 1, 2, 3, 4, 5, 6); } catch (E e) { got = e.Code; }
        if (got != 44) bad |= 1 << 6;

        got = -1; try { Args<T>(45, 10); } catch (E e) { got = e.Code; }
        if (got != 45) bad |= 1 << 7;

        got = -1; try { ThrowHere<T>(46); } catch (E e) { got = e.Code; }
        if (got != 46 || ThrowHere<T>(-1) != 0) bad |= 1 << 8;

        // The runtime's throws.
        object str = "zz", notStr = three, strs = new string[1];
        if (!Raises<IndexOutOfRangeException>(() => Helper<T>(0, three, 0, null))) bad |= 1 << 9;
        if (!Raises<DivideByZeroException>(() => Helper<T>(1, three, 0, null))) bad |= 1 << 10;
        if (!Raises<InvalidCastException>(() => Helper<T>(2, three, 0, notStr))) bad |= 1 << 11;
        if (!Raises<OverflowException>(() => Helper<T>(3, three, 0, null))) bad |= 1 << 12;
        if (!Raises<ArrayTypeMismatchException>(() => Helper<T>(4, three, 0, strs))) bad |= 1 << 13;
        if (!Raises<FormatException>(() => Helper<T>(5, three, 0, str))) bad |= 1 << 14;
        if (Helper<T>(6, three, 0, null) != 0) bad |= 1 << 15;

        // By value in and out.
        if (ValueFlow<T>(-1, -1) != 0) bad |= 1 << 16;
        got = -1; try { ValueFlow<T>(47, -1); } catch (E e) { got = e.Code; }
        if (got != 47) bad |= 1 << 17;
        got = -1; try { ValueFlow<T>(-1, 48); } catch (E e) { got = e.Code; }
        if (got != 48) bad |= 1 << 18;

        // Frames with a handler.
        finRuns = 0; finBad = 0;
        got = -1; try { Fin<T>(3, 49); } catch (E e) { got = e.Code; }
        if (got != 49 || finRuns != 4 || finBad != 0) bad |= 1 << 19;
        finRuns = 0;
        if (Fin<T>(3, -1) != 0 || finRuns != 4 || finBad != 0) bad |= 1 << 20;

        if (CatchHere<T>(2, 50) != 50 || CatchHere<T>(2, -1) != 0) bad |= 1 << 21;

        rethrows = 0;
        got = -1; try { Rethrow<T>(2, 51); } catch (E e) { got = e.Code; }
        if (got != 51 || rethrows != 1) bad |= 1 << 22;

        got = -1; try { FinThrows<T>(2, 52, 53); } catch (E e) { got = e.Code; }
        if (got != 53) bad |= 1 << 23;

        if (Filter<T>() != 70 + 100 + 9) bad |= 1 << 24;

        // Generic virtual method and boxed struct.
        Walker w = new DeepWalker();
        got = -1; try { w.Walk<T>(2, 54); } catch (E e) { got = e.Code; }
        if (got != 54 || w.Walk<T>(2, -1) != 0) bad |= 1 << 25;

        IStep s = new BigStep<T> { Pad = default, Seed = 3 };
        got = -1; try { s.Run(2, 55); } catch (E e) { got = e.Code; }
        if (got != 55 || s.Run(2, -1) != 0) bad |= 1 << 26;

        // What this frame kept across all of it.
        if (c0 != 0x1111 || c1 != 0x2222 || c2 != 0x3333 || c3 != 0x4444 ||
            c4 != 0x5555 || c5 != 0x6666 || c6 != 0x7777 || c7 != 0x8888)
            bad |= 1 << 27;

        Check(name + (bad == 0 ? "" : " cases=0x" + bad.ToString("x")), bad == 0);
    }

    // ---- frames of different sizes and kinds on one stack ----

    static int[] plan;
    static Step[] steps;
    static bool[] stepHasFinally;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Next(int i, int code)
    {
        int r = i == plan.Length ? Thrower(code) : steps[plan[i]](i + 1, code);
        return r + 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int PlanThrough<T>(int i, int code) where T : struct
    {
        T buf = default;
        long want = Buf.Fill(ref buf, i + code);
        int r = Next(i, code);
        return r + (Buf.Sum(ref buf) == want ? 0 : 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int PlanRegs<T>(int i, int code) where T : struct
    {
        T buf = default;
        long want = Buf.Fill(ref buf, i + code);
        long x0 = Op(i), x1 = Op(i + 1), x2 = Op(i + 2), x3 = Op(i + 3), x4 = Op(i + 4);
        long x5 = Op(i + 5), x6 = Op(i + 6), x7 = Op(i + 7), x8 = Op(i + 8), x9 = Op(i + 9);
        int r = Next(i, code);
        long xs = x0 + x1 + x2 + x3 + x4 + x5 + x6 + x7 + x8 + x9;
        return r + (xs == Op(10L * i + 45) ? 0 : 1) + (Buf.Sum(ref buf) == want ? 0 : 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int PlanFin<T>(int i, int code) where T : struct
    {
        T buf = default;
        long want = Buf.Fill(ref buf, i + code);
        try
        {
            return Next(i, code);
        }
        finally
        {
            finRuns++;
            if (Buf.Sum(ref buf) != want) finBad++;
        }
    }

    static bool RunPlan(int[] p)
    {
        plan = p;
        int fins = 0;
        foreach (int k in p)
            if (stepHasFinally[k]) fins++;

        finRuns = 0; finBad = 0;
        if (Next(0, -1) != 0 || finRuns != fins || finBad != 0) return false;

        finRuns = 0;
        int got = -1;
        try { Next(0, 60); } catch (E e) { got = e.Code; }
        return got == 60 && finRuns == fins && finBad == 0;
    }

    static void Plans()
    {
        steps = new Step[]
        {
            PlanThrough<B16>, PlanThrough<B2032>, PlanThrough<B2048>, PlanThrough<B2064>,
            PlanThrough<B4096>, PlanThrough<B8192>,
            PlanRegs<B16>, PlanRegs<B2024>, PlanRegs<B2048>, PlanRegs<B4104>, PlanRegs<B8192>,
            PlanFin<B16>, PlanFin<B2048>, PlanFin<B4096>,
        };
        stepHasFinally = new bool[steps.Length];
        for (int i = 11; i < steps.Length; i++) stepHasFinally[i] = true;

        // Every kind alone, then a finally above, between and below large
        // frames, then every pair in both orders.
        bool ok = true;
        for (int i = 0; i < steps.Length; i++)
            ok &= RunPlan(new[] { i });
        Check("plan single", ok);

        Check("plan sandwich", RunPlan(new[] { 11, 4, 12, 5, 13, 2, 11 }));
        Check("plan regs between", RunPlan(new[] { 8, 12, 9, 13, 10, 4 }));

        ok = true;
        for (int i = 0; i < steps.Length; i++)
            for (int j = 0; j < steps.Length; j++)
                ok &= RunPlan(new[] { i, j });
        Check("plan pairs", ok);

        // And stacks picked by a fixed generator.
        ok = true;
        uint seed = 12345;
        for (int n = 0; n < 60; n++)
        {
            seed = seed * 1664525 + 1013904223;
            int len = 3 + (int)((seed >> 16) % 10);
            int[] p = new int[len];
            for (int i = 0; i < len; i++)
            {
                seed = seed * 1664525 + 1013904223;
                p[i] = (int)((seed >> 16) % (uint)steps.Length);
            }
            ok &= RunPlan(p);
        }
        Check("plan generated", ok);
    }

    // ---- many large frames, and many throws ----

    static void Deep()
    {
        int got = -1;
        try { Through<B2048>(40, 61); } catch (E e) { got = e.Code; }
        Check("deep frameless", got == 61 && Through<B2048>(40, -1) == 0);

        finRuns = 0; finBad = 0; got = -1;
        try { Fin<B2064>(30, 62); } catch (E e) { got = e.Code; }
        Check("deep finally", got == 62 && finRuns == 31 && finBad == 0);

        got = -1;
        try { Regs<B4104>(20, 63, 1, 2, 3, 4, 5, 6); } catch (E e) { got = e.Code; }
        Check("deep regs", got == 63);

        bool ok = true;
        for (int i = 0; i < 200; i++)
        {
            got = -1;
            try
            {
                if ((i & 1) == 0) Through<B2056>(i % 5, 1000 + i);
                else Regs<B8192>(i % 3, 1000 + i, i, 2, 3, 4, 5, 6);
            }
            catch (E e) { got = e.Code; }
            ok &= got == 1000 + i;
        }
        Check("repeated throws", ok);
    }

    static int Main()
    {
        Size<B8>("size 8");
        Size<B16>("size 16");
        Size<B64>("size 64");
        Size<B256>("size 256");
        Size<B1024>("size 1024");
        Size<B1536>("size 1536");
        Size<B1904>("size 1904");
        Size<B1920>("size 1920");
        Size<B1936>("size 1936");
        Size<B1952>("size 1952");
        Size<B1968>("size 1968");
        Size<B1976>("size 1976");
        Size<B1984>("size 1984");
        Size<B1992>("size 1992");
        Size<B2000>("size 2000");
        Size<B2008>("size 2008");
        Size<B2016>("size 2016");
        Size<B2024>("size 2024");
        Size<B2032>("size 2032");
        Size<B2040>("size 2040");
        Size<B2048>("size 2048");
        Size<B2056>("size 2056");
        Size<B2064>("size 2064");
        Size<B2072>("size 2072");
        Size<B2080>("size 2080");
        Size<B2096>("size 2096");
        Size<B2128>("size 2128");
        Size<B2304>("size 2304");
        Size<B3072>("size 3072");
        Size<B4080>("size 4080");
        Size<B4088>("size 4088");
        Size<B4096>("size 4096");
        Size<B4104>("size 4104");
        Size<B6144>("size 6144");
        Size<B8192>("size 8192");
        Size<B12288>("size 12288");
        Size<B16384>("size 16384");

        Plans();
        Deep();

        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails;
    }
}
