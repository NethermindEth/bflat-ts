// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Methods compiled without a frame pointer.
//
// A RISC-V method that has no handler and no stackalloc gets no frame
// pointer: its locals, its saved registers and its arguments on the stack
// are addressed from SP, and s0 is one more register for its values. A
// method with a handler or a stackalloc keeps s0 as the frame pointer. So a
// stack is a mix of both kinds, and a method of the first kind can hold any
// value in s0 while a method of the second kind above it relies on s0 being
// its frame. What must hold:
//   - every callee-saved register a method uses is saved and comes back, on
//     return and when an exception unwinds through the method. The unwinder
//     has to find the saved s0 in a frame without a frame pointer, or the
//     handler above runs on a wrong frame;
//   - the offsets from SP are right for every frame layout: any number of
//     saved registers, a frame around 2040 bytes where it is allocated in
//     two steps, an area for outgoing arguments, arguments that came on
//     the stack.
//
// The cases:
//   - one to fourteen values kept across a call: every count of saved
//     registers, s0 among them from twelve on, and the first spills;
//   - the same with a buffer in the frame, for every size in 8-byte steps
//     where the frame crosses 2040 bytes, and five counts of registers;
//   - twenty arguments passed, twelve of them on the stack, to a method
//     that reads them over a frame of its own, small and large;
//   - structs returned through a buffer and passed by reference;
//   - object references and a reference into an array kept across a call
//     that allocates and asks for a collection;
//   - stacks mixed of frames with and without a frame pointer: stackalloc
//     of a variable size, finally, catch and rethrow, a synchronized
//     method, a split frame, stack arguments, in every pair and in
//     generated orders, with and without an exception through them;
//   - an exception thrown in a handler, through such frames, and caught
//     there; three hundred frames deep.
//
// A method that can throw is called with a code: negative for no throw.
//
// Every group of cases runs under a caller that keeps twelve values across
// the call, one for every callee-saved register, and checks them after it.
//
// Each group prints one line, and one more for each case that failed; the
// run ends with ALL PASS or a FAIL count, and the guest returns the number
// of failures.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class E : Exception { public int Code; public E(int c) : base("e") { Code = c; } }

[StructLayout(LayoutKind.Sequential, Size = 16)] struct B16 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 1904)] struct B1904 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 1912)] struct B1912 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 1920)] struct B1920 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 1928)] struct B1928 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 1936)] struct B1936 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 1944)] struct B1944 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 1952)] struct B1952 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 1960)] struct B1960 { public long First; }
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
[StructLayout(LayoutKind.Sequential, Size = 4096)] struct B4096 { public long First; }

struct S40 { public long A, B, C, D, E; }

delegate long Kind(int i, int code);
delegate long Sized(ref long p, int code);
delegate long Passing(long a, int code);

// The buffer of a frame, as words. Taking its address keeps it on the stack.
static class Buf
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Fill<T>(ref T b, long seed) where T : struct
    {
        Span<long> s = MemoryMarshal.CreateSpan(ref Unsafe.As<T, long>(ref b), Unsafe.SizeOf<T>() / 8);
        long sum = 0;
        for (int i = 0; i < s.Length; i++)
        {
            long v = (seed + 1) * 0x100000001B3L + i * 0x9E3779B1L;
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

class Program
{
    static int fails;
    static readonly long[] words = new long[32];

    static int Eq(string name, long got, long want)
    {
        if (got == want) return 0;
        Console.WriteLine("FAIL " + name + " got=" + got + " want=" + want);
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Op(long v) => v;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static long At(ref long p, int i) => Unsafe.Add(ref p, i);

    // Returns 0 for a negative code, so every path can be run without a throw.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Thrower(int code)
    {
        if (code < 0) return 0;
        throw new E(code);
    }

    // Twelve values live across the call: one for every callee-saved register.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Group(string name, Func<int> body)
    {
        long c0 = Op(0x1001), c1 = Op(0x1002), c2 = Op(0x1003), c3 = Op(0x1004);
        long c4 = Op(0x1005), c5 = Op(0x1006), c6 = Op(0x1007), c7 = Op(0x1008);
        long c8 = Op(0x1009), c9 = Op(0x100a), c10 = Op(0x100b), c11 = Op(0x100c);
        int f = body();
        if (c0 != 0x1001 || c1 != 0x1002 || c2 != 0x1003 || c3 != 0x1004 ||
            c4 != 0x1005 || c5 != 0x1006 || c6 != 0x1007 || c7 != 0x1008 ||
            c8 != 0x1009 || c9 != 0x100a || c10 != 0x100b || c11 != 0x100c)
        {
            Console.WriteLine("FAIL " + name + ": a callee-saved register changed");
            f++;
        }
        Console.WriteLine((f == 0 ? "ok   " : "FAIL ") + name);
        fails += f;
    }

    // What a case gives when it throws: the code and two values this frame
    // kept, so a handler that runs on a wrong frame shows.
    const long Thrown = 1000000000;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Try(Func<long> body)
    {
        long k0 = Op(7), k1 = Op(9);
        try
        {
            return body();
        }
        catch (E e)
        {
            return Thrown + e.Code * 100 + k0 * 10 + k1 - 79;
        }
    }

    // ---- values kept across a call ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Keep1(ref long p, int code)
    {
        long v0 = At(ref p, 0);
        if (code == -2) throw new E(77);
        long r = Thrower(code);
        return r + v0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Keep2(ref long p, int code)
    {
        long v0 = At(ref p, 0), v1 = At(ref p, 1);
        if (code == -2) throw new E(77);
        long r = Thrower(code);
        return r + v0 + v1 * 2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Keep3(ref long p, int code)
    {
        long v0 = At(ref p, 0), v1 = At(ref p, 1), v2 = At(ref p, 2);
        if (code == -2) throw new E(77);
        long r = Thrower(code);
        return r + v0 + v1 * 2 + v2 * 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Keep4(ref long p, int code)
    {
        long v0 = At(ref p, 0), v1 = At(ref p, 1), v2 = At(ref p, 2), v3 = At(ref p, 3);
        if (code == -2) throw new E(77);
        long r = Thrower(code);
        return r + v0 + v1 * 2 + v2 * 3 + v3 * 4;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Keep5(ref long p, int code)
    {
        long v0 = At(ref p, 0), v1 = At(ref p, 1), v2 = At(ref p, 2), v3 = At(ref p, 3);
        long v4 = At(ref p, 4);
        if (code == -2) throw new E(77);
        long r = Thrower(code);
        return r + v0 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Keep6(ref long p, int code)
    {
        long v0 = At(ref p, 0), v1 = At(ref p, 1), v2 = At(ref p, 2), v3 = At(ref p, 3);
        long v4 = At(ref p, 4), v5 = At(ref p, 5);
        if (code == -2) throw new E(77);
        long r = Thrower(code);
        return r + v0 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Keep7(ref long p, int code)
    {
        long v0 = At(ref p, 0), v1 = At(ref p, 1), v2 = At(ref p, 2), v3 = At(ref p, 3);
        long v4 = At(ref p, 4), v5 = At(ref p, 5), v6 = At(ref p, 6);
        if (code == -2) throw new E(77);
        long r = Thrower(code);
        return r + v0 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6 + v6 * 7;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Keep8(ref long p, int code)
    {
        long v0 = At(ref p, 0), v1 = At(ref p, 1), v2 = At(ref p, 2), v3 = At(ref p, 3);
        long v4 = At(ref p, 4), v5 = At(ref p, 5), v6 = At(ref p, 6), v7 = At(ref p, 7);
        if (code == -2) throw new E(77);
        long r = Thrower(code);
        return r + v0 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6 + v6 * 7 + v7 * 8;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Keep9(ref long p, int code)
    {
        long v0 = At(ref p, 0), v1 = At(ref p, 1), v2 = At(ref p, 2), v3 = At(ref p, 3);
        long v4 = At(ref p, 4), v5 = At(ref p, 5), v6 = At(ref p, 6), v7 = At(ref p, 7);
        long v8 = At(ref p, 8);
        if (code == -2) throw new E(77);
        long r = Thrower(code);
        return r + v0 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6 + v6 * 7 + v7 * 8 + v8 * 9;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Keep10(ref long p, int code)
    {
        long v0 = At(ref p, 0), v1 = At(ref p, 1), v2 = At(ref p, 2), v3 = At(ref p, 3);
        long v4 = At(ref p, 4), v5 = At(ref p, 5), v6 = At(ref p, 6), v7 = At(ref p, 7);
        long v8 = At(ref p, 8), v9 = At(ref p, 9);
        if (code == -2) throw new E(77);
        long r = Thrower(code);
        return r + v0 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6 + v6 * 7 + v7 * 8 + v8 * 9
               + v9 * 10;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Keep11(ref long p, int code)
    {
        long v0 = At(ref p, 0), v1 = At(ref p, 1), v2 = At(ref p, 2), v3 = At(ref p, 3);
        long v4 = At(ref p, 4), v5 = At(ref p, 5), v6 = At(ref p, 6), v7 = At(ref p, 7);
        long v8 = At(ref p, 8), v9 = At(ref p, 9), v10 = At(ref p, 10);
        if (code == -2) throw new E(77);
        long r = Thrower(code);
        return r + v0 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6 + v6 * 7 + v7 * 8 + v8 * 9
               + v9 * 10 + v10 * 11;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Keep12(ref long p, int code)
    {
        long v0 = At(ref p, 0), v1 = At(ref p, 1), v2 = At(ref p, 2), v3 = At(ref p, 3);
        long v4 = At(ref p, 4), v5 = At(ref p, 5), v6 = At(ref p, 6), v7 = At(ref p, 7);
        long v8 = At(ref p, 8), v9 = At(ref p, 9), v10 = At(ref p, 10), v11 = At(ref p, 11);
        if (code == -2) throw new E(77);
        long r = Thrower(code);
        return r + v0 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6 + v6 * 7 + v7 * 8 + v8 * 9
               + v9 * 10 + v10 * 11 + v11 * 12;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Keep13(ref long p, int code)
    {
        long v0 = At(ref p, 0), v1 = At(ref p, 1), v2 = At(ref p, 2), v3 = At(ref p, 3);
        long v4 = At(ref p, 4), v5 = At(ref p, 5), v6 = At(ref p, 6), v7 = At(ref p, 7);
        long v8 = At(ref p, 8), v9 = At(ref p, 9), v10 = At(ref p, 10), v11 = At(ref p, 11);
        long v12 = At(ref p, 12);
        if (code == -2) throw new E(77);
        long r = Thrower(code);
        return r + v0 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6 + v6 * 7 + v7 * 8 + v8 * 9
               + v9 * 10 + v10 * 11 + v11 * 12 + v12 * 13;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Keep14(ref long p, int code)
    {
        long v0 = At(ref p, 0), v1 = At(ref p, 1), v2 = At(ref p, 2), v3 = At(ref p, 3);
        long v4 = At(ref p, 4), v5 = At(ref p, 5), v6 = At(ref p, 6), v7 = At(ref p, 7);
        long v8 = At(ref p, 8), v9 = At(ref p, 9), v10 = At(ref p, 10), v11 = At(ref p, 11);
        long v12 = At(ref p, 12), v13 = At(ref p, 13);
        if (code == -2) throw new E(77);
        long r = Thrower(code);
        return r + v0 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6 + v6 * 7 + v7 * 8 + v8 * 9
               + v9 * 10 + v10 * 11 + v11 * 12 + v12 * 13 + v13 * 14;
    }

    // ---- the same with a buffer in the frame ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long KeepBuf0<T>(ref long p, int code) where T : struct
    {
        T buf = default;
        long want = Buf.Fill(ref buf, code);
        if (code == -2) throw new E(77);
        long r = Thrower(code);
        return r + (Buf.Sum(ref buf) == want ? 0 : 1000000);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long KeepBuf1<T>(ref long p, int code) where T : struct
    {
        T buf = default;
        long want = Buf.Fill(ref buf, code);
        long v0 = At(ref p, 0);
        if (code == -2) throw new E(77);
        long r = Thrower(code);
        return r + v0 + (Buf.Sum(ref buf) == want ? 0 : 1000000);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long KeepBuf2<T>(ref long p, int code) where T : struct
    {
        T buf = default;
        long want = Buf.Fill(ref buf, code);
        long v0 = At(ref p, 0), v1 = At(ref p, 1);
        if (code == -2) throw new E(77);
        long r = Thrower(code);
        return r + v0 + v1 * 2 + (Buf.Sum(ref buf) == want ? 0 : 1000000);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long KeepBuf5<T>(ref long p, int code) where T : struct
    {
        T buf = default;
        long want = Buf.Fill(ref buf, code);
        long v0 = At(ref p, 0), v1 = At(ref p, 1), v2 = At(ref p, 2), v3 = At(ref p, 3);
        long v4 = At(ref p, 4);
        if (code == -2) throw new E(77);
        long r = Thrower(code);
        return r + v0 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + (Buf.Sum(ref buf) == want ? 0 : 1000000);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long KeepBuf11<T>(ref long p, int code) where T : struct
    {
        T buf = default;
        long want = Buf.Fill(ref buf, code);
        long v0 = At(ref p, 0), v1 = At(ref p, 1), v2 = At(ref p, 2), v3 = At(ref p, 3);
        long v4 = At(ref p, 4), v5 = At(ref p, 5), v6 = At(ref p, 6), v7 = At(ref p, 7);
        long v8 = At(ref p, 8), v9 = At(ref p, 9), v10 = At(ref p, 10);
        if (code == -2) throw new E(77);
        long r = Thrower(code);
        return r + v0 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6 + v6 * 7 + v7 * 8 + v8 * 9
               + v9 * 10 + v10 * 11 + (Buf.Sum(ref buf) == want ? 0 : 1000000);
    }

    // ---- arguments on the stack ----

    // Twenty arguments kept across a call, over a frame with a buffer.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Many<T>(long a1, long a2, long a3, long a4, long a5, long a6, long a7, long a8, long a9, long a10,
                        long a11, long a12, long a13, long a14, long a15, long a16, long a17, long a18, long a19,
                        long a20, int code) where T : struct
    {
        T buf = default;
        long want = Buf.Fill(ref buf, a1);
        long r = Thrower(code);
        return r + a1 + a2 * 2 + a3 * 3 + a4 * 4 + a5 * 5 + a6 * 6 + a7 * 7 + a8 * 8 + a9 * 9 + a10 * 10 +
               a11 * 11 + a12 * 12 + a13 * 13 + a14 * 14 + a15 * 15 + a16 * 16 + a17 * 17 + a18 * 18 + a19 * 19 +
               a20 * 20 + (Buf.Sum(ref buf) == want ? 0 : 1000000);
    }

    // The caller: its frame has the area for the twelve stack arguments.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long PassMany<T>(long a, int code) where T : struct
    {
        long x = a + 1;
        long r = Many<T>(a, a + 1, a + 2, a + 3, a + 4, a + 5, a + 6, a + 7, a + 8, a + 9, a + 10, a + 11, a + 12,
                         a + 13, a + 14, a + 15, a + 16, a + 17, a + 18, a + 19, code);
        return r + x;
    }

    // The same from a frame that has a buffer too.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long PassManyBuf<T>(long a, int code) where T : struct
    {
        T buf = default;
        long want = Buf.Fill(ref buf, a);
        long r = Many<B16>(a, a + 1, a + 2, a + 3, a + 4, a + 5, a + 6, a + 7, a + 8, a + 9, a + 10, a + 11, a + 12,
                           a + 13, a + 14, a + 15, a + 16, a + 17, a + 18, a + 19, code);
        return r + a + 1 + (Buf.Sum(ref buf) == want ? 0 : 1000000);
    }

    // ---- structs ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static S40 MakeStruct(ref long p, int code)
    {
        long v0 = At(ref p, 0), v1 = At(ref p, 1), v2 = At(ref p, 2), v3 = At(ref p, 3), v4 = At(ref p, 4);
        long r = Thrower(code);
        S40 s;
        s.A = v0 + r;
        s.B = v1;
        s.C = v2;
        s.D = v3;
        s.E = v4;
        return s;
    }

    // The struct comes by reference to a copy: the caller's one stays.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long TakeStruct(S40 s, int code)
    {
        long r = Thrower(code);
        s.A += 100;
        return s.A + s.B + s.C + s.D + s.E + r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Structs(ref long p, int code1, int code2)
    {
        S40 s = MakeStruct(ref p, code1);
        long t = TakeStruct(s, code2);
        return t * 1000 + s.A;
    }

    // ---- references ----

    static string[] strings;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Churn(int code)
    {
        for (int i = 0; i < 20; i++)
        {
            byte[] junk = new byte[64 + i];
            junk[i] = 1;
        }
        GC.Collect();
        Thrower(code);
    }

    // Twelve object references and a reference into an array across the call.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long References(string[] src, int code)
    {
        string s0 = src[0], s1 = src[1], s2 = src[2], s3 = src[3], s4 = src[4], s5 = src[5];
        string s6 = src[6], s7 = src[7], s8 = src[8], s9 = src[9], s10 = src[10], s11 = src[11];
        int[] arr = new int[8];
        arr[3] = 33;
        ref int slot = ref arr[3];
        Churn(code);
        slot += 1;
        return s0.Length + s1.Length * 2 + s2.Length * 3 + s3.Length * 4 + s4.Length * 5 + s5.Length * 6 +
               s6.Length * 7 + s7.Length * 8 + s8.Length * 9 + s9.Length * 10 + s10.Length * 11 + s11.Length * 12 +
               arr[3];
    }

    // ---- many frames ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Deep(int n, long a, int code)
    {
        long v1 = a + 1, v2 = a * 2;
        long r = n == 0 ? Thrower(code) : Deep(n - 1, a + 1, code);
        return r + (v1 + v2 == 3 * a + 1 ? 1 : 1000);
    }

    // ---- frames of every kind on one stack ----

    static int[] plan;
    static Kind[] kinds;
    static bool[] kindHasFinally, kindRethrows;
    static int finRuns, finBad, rethrows;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Next(int i, int code)
    {
        long r = i == plan.Length ? Thrower(code) : kinds[plan[i]](i + 1, code);
        return r + 0;
    }

    // Twelve values across the call: s0 holds one of them.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long KindTwelve(int i, int code)
    {
        long v0 = Op(1), v1 = Op(2), v2 = Op(3), v3 = Op(4), v4 = Op(5), v5 = Op(6);
        long v6 = Op(7), v7 = Op(8), v8 = Op(9), v9 = Op(10), v10 = Op(11), v11 = Op(12);
        long r = Next(i, code);
        // Every term needs the result of the call, so none is computed before it.
        long s = (v0 ^ r) + (v1 ^ r) * 2 + (v2 ^ r) * 3 + (v3 ^ r) * 4 + (v4 ^ r) * 5 + (v5 ^ r) * 6 +
                 (v6 ^ r) * 7 + (v7 ^ r) * 8 + (v8 ^ r) * 9 + (v9 ^ r) * 10 + (v10 ^ r) * 11 + (v11 ^ r) * 12;
        return r + (s == 650 ? 0 : 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long KindThree(int i, int code)
    {
        long v0 = Op(1), v1 = Op(2), v2 = Op(3);
        long r = Next(i, code);
        return r + ((v0 ^ r) + (v1 ^ r) * 2 + (v2 ^ r) * 3 == 14 ? 0 : 1);
    }

    // stackalloc of a size that depends on the place in the stack.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long KindAlloca(int i, int code)
    {
        int n = i * 5 + 3;
        Span<long> s = stackalloc long[n];
        for (int k = 0; k < n; k++) s[k] = k * 7 + i;
        long r = Next(i, code);
        long sum = 0;
        for (int k = 0; k < n; k++) sum += s[k];
        return r + (sum == 7L * n * (n - 1) / 2 + (long)n * i ? 0 : 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long KindFinally(int i, int code)
    {
        long local = Op(i + 500);
        try
        {
            return Next(i, code);
        }
        finally
        {
            finRuns++;
            if (local != i + 500) finBad++;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long KindRethrow(int i, int code)
    {
        long local = Op(i + 600);
        try
        {
            return Next(i, code);
        }
        catch (E)
        {
            if (local == i + 600) rethrows++;
            throw;
        }
    }

    // A frame over 2040 bytes: allocated in two steps.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long KindLarge(int i, int code)
    {
        B2048 buf = default;
        long want = Buf.Fill(ref buf, i);
        long r = Next(i, code);
        return r + (Buf.Sum(ref buf) == want ? 0 : 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Sink(long a1, long a2, long a3, long a4, long a5, long a6, long a7, long a8,
                     long a9, long a10, long a11, long a12, int i, int code)
    {
        long r = Next(i, code);
        return r + (a1 + a2 + a3 + a4 + a5 + a6 + a7 + a8 + a9 + a10 + a11 + a12 == 12 * a1 + 66 ? 0 : 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long KindArgs(int i, int code)
    {
        long k = i * 3;
        long r = Sink(k, k + 1, k + 2, k + 3, k + 4, k + 5, k + 6, k + 7, k + 8, k + 9, k + 10, k + 11, i, code);
        return r + (k == i * 3 ? 0 : 1);
    }

    [MethodImpl(MethodImplOptions.Synchronized | MethodImplOptions.NoInlining)]
    static long KindSynchronized(int i, int code)
    {
        long local = Op(i + 700);
        long r = Next(i, code);
        return r + (local == i + 700 ? 0 : 1);
    }

    static bool RunPlan(int[] p)
    {
        plan = p;
        int fins = 0, rets = 0;
        foreach (int k in p)
        {
            if (kindHasFinally[k]) fins++;
            if (kindRethrows[k]) rets++;
        }

        finRuns = 0; finBad = 0; rethrows = 0;
        if (Next(0, -1) != 0 || finRuns != fins || finBad != 0 || rethrows != 0) return false;

        finRuns = 0;
        long got = Try(() => Next(0, 60));
        return got == Thrown + 6000 && finRuns == fins && finBad == 0 && rethrows == rets;
    }

    // ---- an exception in a handler ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long InHandler(ref long p)
    {
        long local = Op(40);
        try
        {
            Keep12(ref p, 5);
        }
        catch (E e)
        {
            long inner;
            try
            {
                inner = Keep14(ref p, 6);
            }
            catch (E e2)
            {
                inner = e2.Code;
            }
            return e.Code * 100 + inner * 10 + (Keep13(ref p, -1) == 819 ? 1 : 0) + local;
        }
        return -1;
    }

    // ---- the groups ----

    static int Kept()
    {
        int f = 0;
        ref long p = ref words[0];
        f += Eq("keep 1", Keep1(ref p, -1), 1);
        f += Eq("keep 1 throws", Try(() => Keep1(ref words[0], 1)), Thrown + 100);
        f += Eq("keep 1 throws itself", Try(() => Keep1(ref words[0], -2)), Thrown + 7700);
        f += Eq("keep 2", Keep2(ref p, -1), 5);
        f += Eq("keep 2 throws", Try(() => Keep2(ref words[0], 2)), Thrown + 200);
        f += Eq("keep 2 throws itself", Try(() => Keep2(ref words[0], -2)), Thrown + 7700);
        f += Eq("keep 3", Keep3(ref p, -1), 14);
        f += Eq("keep 3 throws", Try(() => Keep3(ref words[0], 3)), Thrown + 300);
        f += Eq("keep 3 throws itself", Try(() => Keep3(ref words[0], -2)), Thrown + 7700);
        f += Eq("keep 4", Keep4(ref p, -1), 30);
        f += Eq("keep 4 throws", Try(() => Keep4(ref words[0], 4)), Thrown + 400);
        f += Eq("keep 4 throws itself", Try(() => Keep4(ref words[0], -2)), Thrown + 7700);
        f += Eq("keep 5", Keep5(ref p, -1), 55);
        f += Eq("keep 5 throws", Try(() => Keep5(ref words[0], 5)), Thrown + 500);
        f += Eq("keep 5 throws itself", Try(() => Keep5(ref words[0], -2)), Thrown + 7700);
        f += Eq("keep 6", Keep6(ref p, -1), 91);
        f += Eq("keep 6 throws", Try(() => Keep6(ref words[0], 6)), Thrown + 600);
        f += Eq("keep 6 throws itself", Try(() => Keep6(ref words[0], -2)), Thrown + 7700);
        f += Eq("keep 7", Keep7(ref p, -1), 140);
        f += Eq("keep 7 throws", Try(() => Keep7(ref words[0], 7)), Thrown + 700);
        f += Eq("keep 7 throws itself", Try(() => Keep7(ref words[0], -2)), Thrown + 7700);
        f += Eq("keep 8", Keep8(ref p, -1), 204);
        f += Eq("keep 8 throws", Try(() => Keep8(ref words[0], 8)), Thrown + 800);
        f += Eq("keep 8 throws itself", Try(() => Keep8(ref words[0], -2)), Thrown + 7700);
        f += Eq("keep 9", Keep9(ref p, -1), 285);
        f += Eq("keep 9 throws", Try(() => Keep9(ref words[0], 9)), Thrown + 900);
        f += Eq("keep 9 throws itself", Try(() => Keep9(ref words[0], -2)), Thrown + 7700);
        f += Eq("keep 10", Keep10(ref p, -1), 385);
        f += Eq("keep 10 throws", Try(() => Keep10(ref words[0], 10)), Thrown + 1000);
        f += Eq("keep 10 throws itself", Try(() => Keep10(ref words[0], -2)), Thrown + 7700);
        f += Eq("keep 11", Keep11(ref p, -1), 506);
        f += Eq("keep 11 throws", Try(() => Keep11(ref words[0], 11)), Thrown + 1100);
        f += Eq("keep 11 throws itself", Try(() => Keep11(ref words[0], -2)), Thrown + 7700);
        f += Eq("keep 12", Keep12(ref p, -1), 650);
        f += Eq("keep 12 throws", Try(() => Keep12(ref words[0], 12)), Thrown + 1200);
        f += Eq("keep 12 throws itself", Try(() => Keep12(ref words[0], -2)), Thrown + 7700);
        f += Eq("keep 13", Keep13(ref p, -1), 819);
        f += Eq("keep 13 throws", Try(() => Keep13(ref words[0], 13)), Thrown + 1300);
        f += Eq("keep 13 throws itself", Try(() => Keep13(ref words[0], -2)), Thrown + 7700);
        f += Eq("keep 14", Keep14(ref p, -1), 1015);
        f += Eq("keep 14 throws", Try(() => Keep14(ref words[0], 14)), Thrown + 1400);
        f += Eq("keep 14 throws itself", Try(() => Keep14(ref words[0], -2)), Thrown + 7700);
        return f;
    }

    static int Buffers()
    {
        int f = 0;
        ref long p = ref words[0];
        for (int i = 0; i < keepBuf.Length; i++)
        {
            int at = i;
            f += Eq("buffer " + i, keepBuf[i](ref p, -1), keepBufWant[i]);
            f += Eq("buffer " + i + " throws", Try(() => keepBuf[at](ref words[0], 61)), Thrown + 6100);
            f += Eq("buffer " + i + " throws itself", Try(() => keepBuf[at](ref words[0], -2)), Thrown + 7700);
        }
        return f;
    }

    static int Arguments()
    {
        int f = 0;
        for (int i = 0; i < passMany.Length; i++)
        {
            int at = i;
            f += Eq("arguments " + i, passMany[i](3, -1), 3 * 210 + 2660 + 4);
            f += Eq("arguments " + i + " throw", Try(() => passMany[at](3, 62)), Thrown + 6200);
        }
        return f;
    }

    static int Values()
    {
        int f = 0;
        ref long p = ref words[0];
        f += Eq("structs", Structs(ref p, -1, -1), 115001);
        f += Eq("structs throw 1", Try(() => Structs(ref words[0], 63, -1)), Thrown + 6300);
        f += Eq("structs throw 2", Try(() => Structs(ref words[0], -1, 64)), Thrown + 6400);
        f += Eq("references", References(strings, -1), 650 + 34);
        f += Eq("references throw", Try(() => References(strings, 65)), Thrown + 6500);
        f += Eq("deep", Deep(300, 5, -1), 301);
        f += Eq("deep throws", Try(() => Deep(300, 5, 66)), Thrown + 6600);
        f += Eq("in handler", InHandler(ref p), 500 + 60 + 1 + 40);
        long sum = 0;
        for (int i = 0; i < 100; i++)
        {
            int at = i;
            sum += Try(() => (at & 1) == 0 ? Keep12(ref words[0], at) : Keep14(ref words[0], at)) - Thrown;
        }
        f += Eq("repeated throws", sum, 495000);
        return f;
    }

    static int Mixed()
    {
        kinds = new Kind[]
        {
            KindTwelve, KindThree, KindAlloca, KindFinally, KindRethrow, KindLarge, KindArgs, KindSynchronized,
        };
        kindHasFinally = new bool[kinds.Length];
        kindRethrows = new bool[kinds.Length];
        kindHasFinally[3] = true;
        kindRethrows[4] = true;

        int f = 0;
        bool ok = true;
        for (int i = 0; i < kinds.Length; i++)
            ok &= RunPlan(new[] { i });
        f += Eq("plan single", ok ? 1 : 0, 1);

        // A handler above and below every frame that uses s0 for a value.
        f += Eq("plan sandwich", RunPlan(new[] { 3, 0, 4, 0, 2, 0, 7, 0, 3 }) ? 1 : 0, 1);
        f += Eq("plan large between", RunPlan(new[] { 5, 3, 5, 2, 5, 0, 5, 4 }) ? 1 : 0, 1);

        ok = true;
        for (int i = 0; i < kinds.Length; i++)
            for (int j = 0; j < kinds.Length; j++)
                ok &= RunPlan(new[] { i, j });
        f += Eq("plan pairs", ok ? 1 : 0, 1);

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
                p[i] = (int)((seed >> 16) % (uint)kinds.Length);
            }
            ok &= RunPlan(p);
        }
        f += Eq("plan generated", ok ? 1 : 0, 1);
        return f;
    }

    static Sized[] keepBuf;
    static long[] keepBufWant;
    static Passing[] passMany;

    static void Setup()
    {
        for (int i = 0; i < words.Length; i++) words[i] = i + 1;
        strings = new string[12];
        for (int i = 0; i < strings.Length; i++) strings[i] = new string('x', i + 1);
        keepBuf = new Sized[]
        {
            KeepBuf0<B1904>, KeepBuf0<B1912>, KeepBuf0<B1920>, KeepBuf0<B1928>, KeepBuf0<B1936>,
            KeepBuf0<B1944>, KeepBuf0<B1952>, KeepBuf0<B1960>, KeepBuf0<B1968>, KeepBuf0<B1976>,
            KeepBuf0<B1984>, KeepBuf0<B1992>, KeepBuf0<B2000>, KeepBuf0<B2008>, KeepBuf0<B2016>,
            KeepBuf0<B2024>, KeepBuf0<B2032>, KeepBuf0<B2040>, KeepBuf0<B2048>, KeepBuf0<B2056>,
            KeepBuf0<B2064>,
            KeepBuf1<B1904>, KeepBuf1<B1912>, KeepBuf1<B1920>, KeepBuf1<B1928>, KeepBuf1<B1936>,
            KeepBuf1<B1944>, KeepBuf1<B1952>, KeepBuf1<B1960>, KeepBuf1<B1968>, KeepBuf1<B1976>,
            KeepBuf1<B1984>, KeepBuf1<B1992>, KeepBuf1<B2000>, KeepBuf1<B2008>, KeepBuf1<B2016>,
            KeepBuf1<B2024>, KeepBuf1<B2032>, KeepBuf1<B2040>, KeepBuf1<B2048>, KeepBuf1<B2056>,
            KeepBuf1<B2064>,
            KeepBuf2<B1904>, KeepBuf2<B1912>, KeepBuf2<B1920>, KeepBuf2<B1928>, KeepBuf2<B1936>,
            KeepBuf2<B1944>, KeepBuf2<B1952>, KeepBuf2<B1960>, KeepBuf2<B1968>, KeepBuf2<B1976>,
            KeepBuf2<B1984>, KeepBuf2<B1992>, KeepBuf2<B2000>, KeepBuf2<B2008>, KeepBuf2<B2016>,
            KeepBuf2<B2024>, KeepBuf2<B2032>, KeepBuf2<B2040>, KeepBuf2<B2048>, KeepBuf2<B2056>,
            KeepBuf2<B2064>,
            KeepBuf5<B1904>, KeepBuf5<B1912>, KeepBuf5<B1920>, KeepBuf5<B1928>, KeepBuf5<B1936>,
            KeepBuf5<B1944>, KeepBuf5<B1952>, KeepBuf5<B1960>, KeepBuf5<B1968>, KeepBuf5<B1976>,
            KeepBuf5<B1984>, KeepBuf5<B1992>, KeepBuf5<B2000>, KeepBuf5<B2008>, KeepBuf5<B2016>,
            KeepBuf5<B2024>, KeepBuf5<B2032>, KeepBuf5<B2040>, KeepBuf5<B2048>, KeepBuf5<B2056>,
            KeepBuf5<B2064>,
            KeepBuf11<B1904>, KeepBuf11<B1912>, KeepBuf11<B1920>, KeepBuf11<B1928>, KeepBuf11<B1936>,
            KeepBuf11<B1944>, KeepBuf11<B1952>, KeepBuf11<B1960>, KeepBuf11<B1968>, KeepBuf11<B1976>,
            KeepBuf11<B1984>, KeepBuf11<B1992>, KeepBuf11<B2000>, KeepBuf11<B2008>, KeepBuf11<B2016>,
            KeepBuf11<B2024>, KeepBuf11<B2032>, KeepBuf11<B2040>, KeepBuf11<B2048>, KeepBuf11<B2056>,
            KeepBuf11<B2064>,
        };
        keepBufWant = new long[keepBuf.Length];
        for (int i = 0; i < keepBufWant.Length; i++)
            keepBufWant[i] = (i / 21) switch { 0 => 0, 1 => 1, 2 => 5, 3 => 55, _ => 506 };
        passMany = new Passing[]
        {
            PassMany<B16>, PassMany<B2024>, PassMany<B2032>, PassMany<B2040>,
            PassMany<B2048>, PassMany<B4096>, PassManyBuf<B16>, PassManyBuf<B1912>,
            PassManyBuf<B1920>, PassManyBuf<B1928>, PassManyBuf<B1936>, PassManyBuf<B1944>,
            PassManyBuf<B2048>, PassManyBuf<B4096>,
        };
    }

    static int Main()
    {
        Setup();

        Group("values kept across a call", Kept);
        Group("buffers", Buffers);
        Group("stack arguments", Arguments);
        Group("structs, references, depth", Values);
        Group("mixed stacks", Mixed);

        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails;
    }
}
