// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Copies and fills whose length is only known at run time.
//
// Span.CopyTo, Buffer.MemoryCopy, Array.Copy, Unsafe.CopyBlock and the
// memcpy helper of a large struct copy all end in SpanHelpers.Memmove; Clear,
// Fill and InitBlock end in the clearing and filling helpers. Each of them
// picks a path by length: a few overlapping loads and stores for the
// shortest, a loop of words, and above a threshold the native memmove or
// memset. On RISC-V the thresholds are low (16 bytes for a copy, 64 for a
// clear), the threshold test comes before the overlap test, and on ZisK the
// native routine is a precompile instruction pair emitted in place of the
// call, also for a memmove, memcpy or memset the program imports itself.
// What can go wrong: a length at a threshold that falls between two paths,
// a copy between overlapping buffers that takes a path which reads bytes it
// has already written (in either direction), a precompile that is given the
// operands in the wrong registers or a zero length, a tail of one to seven
// bytes that is lost, bytes written before the destination or after its
// end, and a fill value that is not replicated.
//
// Every operation runs for every length from 0 to 140 and for lengths on
// both sides of 256, 512, 1024, 2048 and 4096, with the source and the
// destination at different distances from an 8-byte boundary. Copies also
// run inside one buffer with the destination before and after the source at
// distances from one byte to the length and one more.
//
// The expected bytes come from loops that move one byte at a time and are
// compiled without optimization; a destination is compared together with 32
// bytes before and after it. Each group prints one line; the run ends with
// ALL PASS or a FAIL count.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Byte loops the JIT must not turn into the operations under test.
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
    public static void Set(byte* d, byte v, int n)
    {
        for (int i = 0; i < n; i++)
            d[i] = v;
    }

    // The index of the first difference, -1 if there is none.
    [MethodImpl(Plain)]
    public static int Diff(byte* a, byte* b, int n)
    {
        for (int i = 0; i < n; i++)
        {
            if (a[i] != b[i])
                return i;
        }
        return -1;
    }
}

// The operations under test.
static unsafe class Ops
{
    [DllImport("*")]
    static extern void* memmove(void* d, void* s, nuint n);

    [DllImport("*")]
    static extern void* memcpy(void* d, void* s, nuint n);

    [DllImport("*")]
    static extern void* memset(void* d, int c, nuint n);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SpanCopy(byte* d, byte* s, int n) => new ReadOnlySpan<byte>(s, n).CopyTo(new Span<byte>(d, n));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void BufferCopy(byte* d, byte* s, int n) => Buffer.MemoryCopy(s, d, n, n);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void BlockCopy(byte* d, byte* s, int n) => Unsafe.CopyBlockUnaligned(d, s, (uint)n);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void NativeCopy(byte* d, byte* s, int n) => NativeMemory.Copy(s, d, (nuint)n);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void TryCopy(byte* d, byte* s, int n) => new ReadOnlySpan<byte>(s, n).TryCopyTo(new Span<byte>(d, n + 3));

    // The result of the imported routines is the destination.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long ImportedMemmove(byte* d, byte* s, int n) => (byte*)memmove(d, s, (nuint)n) - d;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long ImportedMemcpy(byte* d, byte* s, int n) => (byte*)memcpy(d, s, (nuint)n) - d;

    // The operands of the imported routine arrive in other registers than it takes them in.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long ImportedMemmoveSwapped(long n, byte* s, byte* d, long k)
    {
        memmove(d, s, (nuint)n);
        return k + n;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SpanClear(byte* d, int n) => new Span<byte>(d, n).Clear();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SpanFill(byte* d, int n, byte v) => new Span<byte>(d, n).Fill(v);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void BlockInit(byte* d, int n, byte v) => Unsafe.InitBlockUnaligned(d, v, (uint)n);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void BlockInitZero(byte* d, int n) => Unsafe.InitBlockUnaligned(d, 0, (uint)n);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void NativeClear(byte* d, int n) => NativeMemory.Clear(d, (nuint)n);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void NativeFill(byte* d, int n, byte v) => NativeMemory.Fill(d, (nuint)n, v);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long ImportedMemset(byte* d, int n, byte v) => (byte*)memset(d, v, (nuint)n) - d;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ArrayCopy(byte[] d, int di, byte[] s, int si, int n) => Array.Copy(s, si, d, di, n);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ArrayBlockCopy(byte[] d, int di, byte[] s, int si, int n) => Buffer.BlockCopy(s, si, d, di, n);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ArraySpanCopy(byte[] d, int di, byte[] s, int si, int n) => s.AsSpan(si, n).CopyTo(d.AsSpan(di));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ArrayClear(byte[] d, int di, int n) => Array.Clear(d, di, n);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LongCopy(long[] d, int di, long[] s, int si, int n) => Array.Copy(s, si, d, di, n);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LongSpanCopy(long[] d, int di, long[] s, int si, int n) => s.AsSpan(si, n).CopyTo(d.AsSpan(di));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LongClear(long[] d, int di, int n) => d.AsSpan(di, n).Clear();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LongFill(long[] d, int di, int n, long v) => d.AsSpan(di, n).Fill(v);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ObjectCopy(object[] d, int di, object[] s, int si, int n) => Array.Copy(s, si, d, di, n);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ObjectClear(object[] d, int di, int n) => d.AsSpan(di, n).Clear();
}

static unsafe class Program
{
    const int Guard = 32;
    const int Room = 4400;
    const int CopyOps = 8;
    const int FillOps = 7;

    static byte* s_src;
    static byte* s_dst;
    static byte* s_exp;
    static byte* s_tmp;
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

    static void Fail(string name, int n, int sm, int dm, long expected, long got, int at)
    {
        s_groupFails++;
        if (s_groupFails <= 6)
        {
            Console.WriteLine("FAIL " + name + " length " + n + " src+" + sm + " dst+" + dm + " expected " + expected +
                              " got " + got + " at offset " + at);
        }
    }

    static string CopyName(int op) => op switch
    {
        0 => "SpanCopy", 1 => "BufferCopy", 2 => "BlockCopy", 3 => "NativeCopy", 4 => "TryCopy",
        5 => "ImportedMemmove", 6 => "ImportedMemcpy", _ => "ImportedMemmoveSwapped",
    };

    static string FillName(int op) => op switch
    {
        0 => "SpanClear", 1 => "SpanFill", 2 => "BlockInit", 3 => "BlockInitZero", 4 => "NativeClear",
        5 => "NativeFill", _ => "ImportedMemset",
    };

    static long DoCopy(int op, byte* d, byte* s, int n)
    {
        switch (op)
        {
            case 0: Ops.SpanCopy(d, s, n); return 0;
            case 1: Ops.BufferCopy(d, s, n); return 0;
            case 2: Ops.BlockCopy(d, s, n); return 0;
            case 3: Ops.NativeCopy(d, s, n); return 0;
            case 4: Ops.TryCopy(d, s, n); return 0;
            case 5: return Ops.ImportedMemmove(d, s, n);
            case 6: return Ops.ImportedMemcpy(d, s, n);
            default: return Ops.ImportedMemmoveSwapped(n, s, d, 1000) - 1000 - n;
        }
    }

    // A copy between two buffers.
    static void Copy(int op, int n, int sm, int dm)
    {
        int span = n + 2 * Guard + 8;
        byte* s = s_src + sm;
        byte* d = s_dst + dm;
        Ref.Fill(s_src - Guard, span, n + op);
        Ref.Fill(s_dst - Guard, span, n + op + 101);
        Ref.Copy(s_exp - Guard, s_dst - Guard, span);
        Ref.Copy(s_tmp - Guard, s_src - Guard, span);
        Ref.Copy(s_exp + dm, s, n);

        long result = DoCopy(op, d, s, n);

        int at = Ref.Diff(s_exp - Guard, s_dst - Guard, span);
        if (at >= 0)
            Fail(CopyName(op), n, sm, dm, s_exp[at - Guard], s_dst[at - Guard], at - Guard - dm);
        at = Ref.Diff(s_tmp - Guard, s_src - Guard, span);
        if (at >= 0)
            Fail(CopyName(op) + " (source changed)", n, sm, dm, s_tmp[at - Guard], s_src[at - Guard], at - Guard - sm);
        if (result != 0)
            Fail(CopyName(op) + " (result)", n, sm, dm, 0, result, 0);
    }

    // A copy inside one buffer; the destination starts `delta` bytes after the source.
    static void Overlap(int op, int n, int sm, int delta)
    {
        int span = 3 * n + 2 * Guard + 16;
        byte* s = s_dst + n + 1 + sm;
        byte* d = s + delta;
        Ref.Fill(s_dst - Guard, span, n + delta);
        Ref.Copy(s_exp - Guard, s_dst - Guard, span);
        Ref.Copy(s_tmp, s, n);
        Ref.Copy(s_exp + (d - s_dst), s_tmp, n);

        long result = DoCopy(op, d, s, n);

        int at = Ref.Diff(s_exp - Guard, s_dst - Guard, span);
        if (at >= 0)
        {
            s_groupFails++;
            if (s_groupFails <= 6)
            {
                Console.WriteLine("FAIL " + CopyName(op) + " overlapping length " + n + " src+" + sm + " distance " + delta +
                                  " expected " + s_exp[at - Guard] + " got " + s_dst[at - Guard] + " at offset " +
                                  (at - Guard - (int)(d - s_dst)));
            }
        }
        if (result != 0)
            Fail(CopyName(op) + " overlapping (result)", n, sm, delta, 0, result, 0);
    }

    static void Fill(int op, int n, int dm)
    {
        int span = n + 2 * Guard + 8;
        byte* d = s_dst + dm;
        byte v = (byte)(0x81 + n * 3);
        Ref.Fill(s_dst - Guard, span, n + op + 55);
        Ref.Copy(s_exp - Guard, s_dst - Guard, span);
        long result = 0;
        switch (op)
        {
            case 0: Ref.Set(s_exp + dm, 0, n); Ops.SpanClear(d, n); break;
            case 1: Ref.Set(s_exp + dm, v, n); Ops.SpanFill(d, n, v); break;
            case 2: Ref.Set(s_exp + dm, v, n); Ops.BlockInit(d, n, v); break;
            case 3: Ref.Set(s_exp + dm, 0, n); Ops.BlockInitZero(d, n); break;
            case 4: Ref.Set(s_exp + dm, 0, n); Ops.NativeClear(d, n); break;
            case 5: Ref.Set(s_exp + dm, v, n); Ops.NativeFill(d, n, v); break;
            default: Ref.Set(s_exp + dm, v, n); result = Ops.ImportedMemset(d, n, v); break;
        }

        int at = Ref.Diff(s_exp - Guard, s_dst - Guard, span);
        if (at >= 0)
            Fail(FillName(op), n, 0, dm, s_exp[at - Guard], s_dst[at - Guard], at - Guard - dm);
        if (result != 0)
            Fail(FillName(op) + " (result)", n, 0, dm, 0, result, 0);
    }

    static bool IsImported(int op) => op >= 5;

    static void Lengths(int from, int to)
    {
        for (int n = from; n <= to; n++)
        {
            for (int op = 0; op < CopyOps; op++)
            {
                for (int i = 0; i < 8; i++)
                {
                    Copy(op, n, i, i);
                    Copy(op, n, i, (i * 3 + 1) & 7);
                    Copy(op, n, 0, i);
                }
            }
            for (int op = 0; op < FillOps; op++)
            {
                for (int i = 0; i < 8; i++)
                    Fill(op, n, i);
            }
        }
    }

    static void Overlaps(int from, int to)
    {
        for (int n = from; n <= to; n++)
        {
            for (int op = 0; op < CopyOps; op++)
            {
                // memcpy does not promise anything for overlapping buffers.
                if (op == 6)
                    continue;
                for (int sm = 0; sm < 8; sm += 3)
                {
                    for (int delta = -n - 1; delta <= n + 1; delta++)
                    {
                        int a = (delta < 0) ? -delta : delta;
                        if ((n > 24) && (a > 9) && (a < n - 9) && ((a % 23) != 0))
                            continue;
                        Overlap(op, n, sm, delta);
                    }
                }
            }
        }
    }

    // ---- arrays ----

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static long Sum(byte[] a)
    {
        long sum = 0;
        for (int i = 0; i < a.Length; i++)
            sum = sum * 131 + a[i];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static long Sum(long[] a)
    {
        long sum = 0;
        for (int i = 0; i < a.Length; i++)
            sum = sum * 131 + a[i];
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static long Sum(object[] a, object[] pool)
    {
        long sum = 0;
        for (int i = 0; i < a.Length; i++)
        {
            int which = -1;
            for (int j = 0; j < pool.Length; j++)
            {
                if (ReferenceEquals(a[i], pool[j]))
                    which = j;
            }
            sum = sum * 131 + ((a[i] == null) ? 7 : which + 11);
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static void Pattern(byte[] a, int seed)
    {
        for (int i = 0; i < a.Length; i++)
            a[i] = (byte)(seed * 13 + i * 5 + 3);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static void Pattern(long[] a, long seed)
    {
        for (int i = 0; i < a.Length; i++)
            a[i] = seed * 0x100000001 + i * 0x10001 + 9;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static void Pattern(object[] a, object[] pool, int seed)
    {
        for (int i = 0; i < a.Length; i++)
            a[i] = pool[(i * 3 + seed + 1000) % pool.Length];
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static void RefMove(byte[] d, int di, byte[] s, int si, int n)
    {
        byte[] t = new byte[n];
        for (int i = 0; i < n; i++)
            t[i] = s[si + i];
        for (int i = 0; i < n; i++)
            d[di + i] = t[i];
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static void RefMove(long[] d, int di, long[] s, int si, int n)
    {
        long[] t = new long[n];
        for (int i = 0; i < n; i++)
            t[i] = s[si + i];
        for (int i = 0; i < n; i++)
            d[di + i] = t[i];
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static void RefMove(object[] d, int di, object[] s, int si, int n)
    {
        object[] t = new object[n];
        for (int i = 0; i < n; i++)
            t[i] = s[si + i];
        for (int i = 0; i < n; i++)
            d[di + i] = t[i];
    }

    static void ArrayFail(string name, int n, int si, int di, long expected, long got)
    {
        s_groupFails++;
        if (s_groupFails <= 6)
            Console.WriteLine("FAIL " + name + " length " + n + " from " + si + " to " + di + " expected " + expected + " got " + got);
    }

    static void Arrays()
    {
        const int Size = 320;
        byte[] a = new byte[Size], b = new byte[Size], ea = new byte[Size], eb = new byte[Size];
        long[] la = new long[Size], lb = new long[Size], lea = new long[Size], leb = new long[Size];
        object[] pool = { new object(), "one", new int[1], new object(), "two" };
        object[] oa = new object[Size], oe = new object[Size];

        for (int n = 0; n <= 70; n++)
        {
            for (int k = 0; k < 6; k++)
            {
                int si = 90 + k, di = 90 + ((k * 5 + 2) % 9);

                for (int op = 0; op < 3; op++)
                {
                    // Between two arrays.
                    Pattern(a, n + op); Pattern(b, n + op + 50);
                    Pattern(ea, n + op); Pattern(eb, n + op + 50);
                    RefMove(eb, di, ea, si, n);
                    if (op == 0) Ops.ArrayCopy(b, di, a, si, n);
                    else if (op == 1) Ops.ArrayBlockCopy(b, di, a, si, n);
                    else Ops.ArraySpanCopy(b, di, a, si, n);
                    if (Sum(eb) != Sum(b) || Sum(ea) != Sum(a))
                        ArrayFail("byte[] copy " + op, n, si, di, Sum(eb), Sum(b));

                    // Inside one array, the destination before and after the source.
                    for (int delta = -n - 1; delta <= n + 1; delta += (n < 12) ? 1 : 1 + n / 6)
                    {
                        Pattern(a, n + op + delta); Pattern(ea, n + op + delta);
                        RefMove(ea, si + delta, ea, si, n);
                        if (op == 0) Ops.ArrayCopy(a, si + delta, a, si, n);
                        else if (op == 1) Ops.ArrayBlockCopy(a, si + delta, a, si, n);
                        else Ops.ArraySpanCopy(a, si + delta, a, si, n);
                        if (Sum(ea) != Sum(a))
                            ArrayFail("byte[] overlapping copy " + op, n, si, si + delta, Sum(ea), Sum(a));
                    }
                }

                Pattern(a, n); Pattern(ea, n);
                for (int i = 0; i < n; i++) ea[di + i] = 0;
                Ops.ArrayClear(a, di, n);
                if (Sum(ea) != Sum(a))
                    ArrayFail("byte[] clear", n, 0, di, Sum(ea), Sum(a));

                for (int op = 0; op < 2; op++)
                {
                    Pattern(la, n + op); Pattern(lb, n + op + 50);
                    Pattern(lea, n + op); Pattern(leb, n + op + 50);
                    RefMove(leb, di, lea, si, n);
                    if (op == 0) Ops.LongCopy(lb, di, la, si, n);
                    else Ops.LongSpanCopy(lb, di, la, si, n);
                    if (Sum(leb) != Sum(lb) || Sum(lea) != Sum(la))
                        ArrayFail("long[] copy " + op, n, si, di, Sum(leb), Sum(lb));

                    for (int delta = -n - 1; delta <= n + 1; delta += (n < 12) ? 1 : 1 + n / 6)
                    {
                        Pattern(la, n + op + delta); Pattern(lea, n + op + delta);
                        RefMove(lea, si + delta, lea, si, n);
                        if (op == 0) Ops.LongCopy(la, si + delta, la, si, n);
                        else Ops.LongSpanCopy(la, si + delta, la, si, n);
                        if (Sum(lea) != Sum(la))
                            ArrayFail("long[] overlapping copy " + op, n, si, si + delta, Sum(lea), Sum(la));
                    }
                }

                Pattern(la, n); Pattern(lea, n);
                for (int i = 0; i < n; i++) lea[di + i] = 0;
                Ops.LongClear(la, di, n);
                if (Sum(lea) != Sum(la))
                    ArrayFail("long[] clear", n, 0, di, Sum(lea), Sum(la));
                for (int i = 0; i < n; i++) lea[si + i] = n * 0x0101010101L + 5;
                Ops.LongFill(la, si, n, n * 0x0101010101L + 5);
                if (Sum(lea) != Sum(la))
                    ArrayFail("long[] fill", n, 0, si, Sum(lea), Sum(la));

                for (int delta = -n - 1; delta <= n + 1; delta += (n < 12) ? 1 : 1 + n / 6)
                {
                    Pattern(oa, pool, n + delta); Pattern(oe, pool, n + delta);
                    RefMove(oe, si + delta, oe, si, n);
                    Ops.ObjectCopy(oa, si + delta, oa, si, n);
                    if (Sum(oe, pool) != Sum(oa, pool))
                        ArrayFail("object[] overlapping copy", n, si, si + delta, Sum(oe, pool), Sum(oa, pool));
                }
                for (int i = 0; i < n; i++) oe[di + i] = null;
                Ops.ObjectClear(oa, di, n);
                if (Sum(oe, pool) != Sum(oa, pool))
                    ArrayFail("object[] clear", n, 0, di, Sum(oe, pool), Sum(oa, pool));
            }
        }
    }

    static int Main()
    {
        byte* mem = stackalloc byte[3 * Room];
        byte* tmp = stackalloc byte[Room];
        s_src = mem + Guard;
        s_dst = mem + Room + Guard;
        s_exp = mem + 2 * Room + Guard;
        s_tmp = tmp + Guard;

        Lengths(0, 140);
        EndGroup("copies and fills of 0 to 140 bytes");

        int[] big = { 255, 256, 257, 511, 512, 513, 1023, 1024, 1025, 2047, 2048, 2049, 4095, 4096, 4097 };
        foreach (int n in big)
            Lengths(n, n);
        EndGroup("copies and fills of 255 to 4097 bytes");

        Overlaps(0, 80);
        int[] mid = { 127, 128, 129, 255, 256, 257, 1024, 1025 };
        foreach (int n in mid)
            Overlaps(n, n);
        EndGroup("copies between overlapping buffers");

        Arrays();
        EndGroup("array copies and clears");

        Console.WriteLine(s_fails == 0 ? "ALL PASS" : "FAILED " + s_fails);
        return s_fails == 0 ? 0 : 1;
    }
}
