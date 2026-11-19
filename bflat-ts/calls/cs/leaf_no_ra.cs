// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Methods that make no call and do not save the return address.
//
// A zkVM guest has one thread and nobody rewrites a return address on the
// stack, so a method that makes no call can leave RA in its register: no
// store in the prolog, no load in the epilog, no slot in the frame, and with
// nothing else to keep no frame at all. That is right only while nothing in
// the method writes RA. The code generator uses RA as a scratch register
// for a few operations, and some operations are calls to a runtime helper
// that the JIT does not show as calls. Such a method has to keep its save.
//
// The cases:
//   - plain arithmetic, compares of every kind and width, compares with
//     constants that do not fit an instruction, several returns;
//   - operations that can write RA or call a helper, now or in an earlier
//     version of the code generator: rotates, checked arithmetic, division,
//     array indexing, copies of structs, stores of object references, a
//     loop;
//   - division by a constant and the high half of a product, table
//     switches, bit counting, byte swaps, interlocked operations;
//   - so many values alive at once that the callee-saved registers are
//     used: they are saved in a frame that has no slot for RA, at every
//     count up to all twelve;
//   - the same with a buffer in the frame, with sizes around 2040 bytes
//     where the frame is allocated in two steps;
//   - arguments read from the caller's frame, structs returned in registers
//     and through a buffer, instance methods.
//
// Every group of cases runs under a caller that keeps twelve values across
// the call, one for every callee-saved register, and checks them after it.
// A method that returns to the wrong place does not come back to report it:
// the run then ends without ALL PASS.
//
// Each group prints one line, and one more for each case that failed; the
// run ends with ALL PASS or a FAIL count, and the guest returns the number
// of failures.
using System;
using System.Numerics;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

[StructLayout(LayoutKind.Sequential, Size = 16)] struct B16 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 1024)] struct B1024 { public long First; }
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

struct S16 { public long A, B; }
struct S24 { public long A, B, C; }
struct S32 { public long A, B, C, D; }
struct S40 { public long A, B, C, D, E; }
struct S64 { public long A, B, C, D, E, F, G, H; }
struct SRef { public object O; public long L; public string S; }

class Holder
{
    public object O;
    public long X;
    public int Y;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public long Get() => X + Y;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Set(long v) { X = v; Y = (int)(v >> 3); }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void SetRef(object o) { O = o; }
}

class Program
{
    static int fails;
    static object staticRef;
    static long staticLong = 40;

    static int Eq(string name, long got, long want)
    {
        if (got == want) return 0;
        Console.WriteLine("FAIL " + name + " got=" + got + " want=" + want);
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Op(long v) => v;

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

    // ---- nothing writes RA: no save expected ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Arith(long a, long b) => (a + b * 3 - (a >> 2)) ^ (b << 5) | (a & 0xff);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Arith32(int a, int b) => (a + b * 3) ^ (a >> 3) ^ (int)((uint)b >> 2);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long ShiftVar(long a, int n) => (a << n) + (a >> n) + (long)((ulong)a >> n);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int CmpSigned(long a, long b) =>
        (a < b ? 1 : 0) | (a <= b ? 2 : 0) | (a > b ? 4 : 0) | (a >= b ? 8 : 0) | (a == b ? 16 : 0) | (a != b ? 32 : 0);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int CmpUnsigned(ulong a, ulong b) =>
        (a < b ? 1 : 0) | (a <= b ? 2 : 0) | (a > b ? 4 : 0) | (a >= b ? 8 : 0);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int CmpInt(int a, int b) =>
        (a < b ? 1 : 0) | (a <= b ? 2 : 0) | (a > b ? 4 : 0) | (a >= b ? 8 : 0);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int CmpUInt(uint a, uint b) =>
        (a < b ? 1 : 0) | (a <= b ? 2 : 0) | (a > b ? 4 : 0) | (a >= b ? 8 : 0);

    // The int is the low half of a long: the compare has to extend it first.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int CmpLowHalf(long a, long b) => (int)a < (int)b ? 1 : ((uint)a > (uint)b ? 2 : 3);

    // Constants that do not fit the 12 bits of an instruction.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int CmpBigConst(long a) =>
        (a < 0x123456789AB ? 1 : 0) | (a > -5000000000 ? 2 : 0) | (a >= 100000 ? 4 : 0) | (a <= 2047 ? 8 : 0) |
        (a < 2048 ? 16 : 0) | (a > -2049 ? 32 : 0);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int CmpBigConstInt(int a) =>
        (a < 1000000 ? 1 : 0) | (a > -1000000 ? 2 : 0) | ((uint)a >= 3000000000u ? 4 : 0) | (a != 123456 ? 8 : 0);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Branches(long a, long b)
    {
        if (a < 0) return -1;
        if (a < 10) return b + 1;
        if (a < 100000) return b * 2;
        if (b > a) return a - b;
        return a ^ b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Select(long a, long b) => Math.Max(a, b) * 3 + Math.Min(a, b) + Math.Abs(a - b);

    // ---- division by a constant, high half of a product ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long DivConst(long a) => a / 7 + a % 1000 + (long)((ulong)a / 10) + (int)a / 3 + (long)((uint)a / 5);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long MulHigh(ulong a, ulong b) => (long)Math.BigMul(a, b, out ulong low) + (long)low;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long MulHighSigned(long a, long b) => Math.BigMul(a, b, out long low) * 1000 + low;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long MulWide(int a, int b) => (long)a * b + (long)((ulong)(uint)a * (uint)b);

    // ---- table switch ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Switch(long a, long b)
    {
        switch (a)
        {
            case 0: return b + 1;
            case 1: return b * 3;
            case 2: return b ^ 0x55;
            case 3: return b >> 2;
            case 4: return -b;
            case 5: return b << 3;
            case 6: return b - 9;
            case 7: return b | 0x100;
            default: return 0;
        }
    }

    // ---- bit operations without the bit-manipulation extension ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Rotates(ulong a, int n) =>
        (long)(BitOperations.RotateLeft(a, n) ^ BitOperations.RotateRight(a, 3) ^ BitOperations.RotateLeft(a, 17));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Rotates32(uint a, int n) =>
        BitOperations.RotateLeft(a, n) ^ BitOperations.RotateRight(a, 3) ^ BitOperations.RotateRight(a, n);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long RotateOnly(ulong a) => (long)BitOperations.RotateLeft(a, 4);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long BitCounts(ulong a) =>
        BitOperations.PopCount(a) * 10000 + BitOperations.LeadingZeroCount(a) * 100
               + BitOperations.TrailingZeroCount(a);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Swaps(ulong a) =>
        (long)BinaryPrimitives.ReverseEndianness(a) + BinaryPrimitives.ReverseEndianness((uint)a);

    // ---- interlocked and volatile ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Locked(ref long cell, long v)
    {
        long r = Interlocked.Add(ref cell, v);
        r += Interlocked.Exchange(ref cell, v * 2);
        r += Interlocked.CompareExchange(ref cell, v * 3, v * 2);
        r += Interlocked.CompareExchange(ref cell, 0, -1);
        return r + Volatile.Read(ref cell);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Locked32(ref int cell, int v)
    {
        int r = Interlocked.Increment(ref cell);
        r += Interlocked.Exchange(ref cell, v);
        r += Interlocked.CompareExchange(ref cell, v + 5, v);
        return r + cell;
    }

    // ---- a helper is called, or RA is a scratch register ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long CheckedAdd(long a, long b) => checked(a + b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long CheckedMul(long a, long b) => checked(a * b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int CheckedMul32(int a, int b) => checked(a * b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint CheckedMulU32(uint a, uint b) => checked(a * b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int CheckedNarrow(long a) => checked((int)a);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long DivVar(long a, long b) => a / b + a % b;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Index(long[] arr, int i) => arr[i] + arr[i + 1];

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Loop(int n)
    {
        long s = 0;
        for (int i = 1; i <= n; i++) s += i;
        return s;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Copy16(ref S16 d, ref S16 s) { d = s; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Copy24(ref S24 d, ref S24 s) { d = s; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Copy32(ref S32 d, ref S32 s) { d = s; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Copy40(ref S40 d, ref S40 s) { d = s; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Copy64(ref S64 d, ref S64 s) { d = s; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Clear64(ref S64 d) { d = default; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CopyRef(ref SRef d, ref SRef s) { d = s; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void StoreStatic(object o) { staticRef = o; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void StoreElement(object[] arr, object o) { arr[1] = o; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long ReadStatic(long a) => staticLong + a;

    // ---- structs out, arguments from the caller's frame ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static S16 Pair(long a, long b)
    {
        S16 r;
        r.A = a + 1;
        r.B = b + 2;
        return r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static S40 Five(long a, long b)
    {
        S40 r;
        r.A = a;
        r.B = b;
        r.C = a + b;
        r.D = a - b;
        r.E = a * b;
        return r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long StackArgs(long a1, long a2, long a3, long a4, long a5, long a6, long a7, long a8,
                          long s1, long s2, long s3) =>
        a1 + a2 * 2 + a3 * 3 + a4 * 4 + a5 * 5 + a6 * 6 + a7 * 7 + a8 * 8 + s1 * 100 + s2 * 1000 + s3 * 10000;

    // The same with a frame between SP and the arguments.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long StackArgsBuf<T>(long a1, long a2, long a3, long a4, long a5, long a6, long a7, long a8,
                                long s1, long s2, long s3) where T : struct
    {
        T buf;
        Unsafe.SkipInit(out buf);
        ref long q = ref Unsafe.As<T, long>(ref buf);
        int last = Unsafe.SizeOf<T>() / 8 - 1;
        Volatile.Write(ref q, a1);
        Volatile.Write(ref Unsafe.Add(ref q, last), a8);
        return Volatile.Read(ref q) + a2 * 2 + a3 * 3 + a4 * 4 + a5 * 5 + a6 * 6 + a7 * 7 +
               Volatile.Read(ref Unsafe.Add(ref q, last)) * 8 + s1 * 100 + s2 * 1000 + s3 * 10000;
    }

    // ---- many values alive at once ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Live13(ref long p)
    {
        long v0 = Unsafe.Add(ref p, 0); long v1 = Unsafe.Add(ref p, 1); long v2 = Unsafe.Add(ref p, 2);
        long v3 = Unsafe.Add(ref p, 3); long v4 = Unsafe.Add(ref p, 4); long v5 = Unsafe.Add(ref p, 5);
        long v6 = Unsafe.Add(ref p, 6); long v7 = Unsafe.Add(ref p, 7); long v8 = Unsafe.Add(ref p, 8);
        long v9 = Unsafe.Add(ref p, 9); long v10 = Unsafe.Add(ref p, 10); long v11 = Unsafe.Add(ref p, 11);
        long v12 = Unsafe.Add(ref p, 12);
        long s = v0 * 1 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6 + v6 * 7 + v7 * 8
               + v8 * 9 + v9 * 10 + v10 * 11 + v11 * 12 + v12 * 13;
        long x = (v12 << 0) ^ (v11 << 1) ^ (v10 << 2) ^ (v9 << 3) ^ (v8 << 4) ^ (v7 << 5) ^ (v6 << 6) ^ (v5 << 0)
               ^ (v4 << 1) ^ (v3 << 2) ^ (v2 << 3) ^ (v1 << 4) ^ (v0 << 5);
        return s + x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Live14(ref long p)
    {
        long v0 = Unsafe.Add(ref p, 0); long v1 = Unsafe.Add(ref p, 1); long v2 = Unsafe.Add(ref p, 2);
        long v3 = Unsafe.Add(ref p, 3); long v4 = Unsafe.Add(ref p, 4); long v5 = Unsafe.Add(ref p, 5);
        long v6 = Unsafe.Add(ref p, 6); long v7 = Unsafe.Add(ref p, 7); long v8 = Unsafe.Add(ref p, 8);
        long v9 = Unsafe.Add(ref p, 9); long v10 = Unsafe.Add(ref p, 10); long v11 = Unsafe.Add(ref p, 11);
        long v12 = Unsafe.Add(ref p, 12); long v13 = Unsafe.Add(ref p, 13);
        long s = v0 * 1 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6 + v6 * 7 + v7 * 8
               + v8 * 9 + v9 * 10 + v10 * 11 + v11 * 12 + v12 * 13 + v13 * 14;
        long x = (v13 << 0) ^ (v12 << 1) ^ (v11 << 2) ^ (v10 << 3) ^ (v9 << 4) ^ (v8 << 5) ^ (v7 << 6) ^ (v6 << 0)
               ^ (v5 << 1) ^ (v4 << 2) ^ (v3 << 3) ^ (v2 << 4) ^ (v1 << 5) ^ (v0 << 6);
        return s + x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Live15(ref long p)
    {
        long v0 = Unsafe.Add(ref p, 0); long v1 = Unsafe.Add(ref p, 1); long v2 = Unsafe.Add(ref p, 2);
        long v3 = Unsafe.Add(ref p, 3); long v4 = Unsafe.Add(ref p, 4); long v5 = Unsafe.Add(ref p, 5);
        long v6 = Unsafe.Add(ref p, 6); long v7 = Unsafe.Add(ref p, 7); long v8 = Unsafe.Add(ref p, 8);
        long v9 = Unsafe.Add(ref p, 9); long v10 = Unsafe.Add(ref p, 10); long v11 = Unsafe.Add(ref p, 11);
        long v12 = Unsafe.Add(ref p, 12); long v13 = Unsafe.Add(ref p, 13); long v14 = Unsafe.Add(ref p, 14);
        long s = v0 * 1 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6 + v6 * 7 + v7 * 8
               + v8 * 9 + v9 * 10 + v10 * 11 + v11 * 12 + v12 * 13 + v13 * 14 + v14 * 15;
        long x = (v14 << 0) ^ (v13 << 1) ^ (v12 << 2) ^ (v11 << 3) ^ (v10 << 4) ^ (v9 << 5) ^ (v8 << 6) ^ (v7 << 0)
               ^ (v6 << 1) ^ (v5 << 2) ^ (v4 << 3) ^ (v3 << 4) ^ (v2 << 5) ^ (v1 << 6) ^ (v0 << 0);
        return s + x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Live16(ref long p)
    {
        long v0 = Unsafe.Add(ref p, 0); long v1 = Unsafe.Add(ref p, 1); long v2 = Unsafe.Add(ref p, 2);
        long v3 = Unsafe.Add(ref p, 3); long v4 = Unsafe.Add(ref p, 4); long v5 = Unsafe.Add(ref p, 5);
        long v6 = Unsafe.Add(ref p, 6); long v7 = Unsafe.Add(ref p, 7); long v8 = Unsafe.Add(ref p, 8);
        long v9 = Unsafe.Add(ref p, 9); long v10 = Unsafe.Add(ref p, 10); long v11 = Unsafe.Add(ref p, 11);
        long v12 = Unsafe.Add(ref p, 12); long v13 = Unsafe.Add(ref p, 13); long v14 = Unsafe.Add(ref p, 14);
        long v15 = Unsafe.Add(ref p, 15);
        long s = v0 * 1 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6 + v6 * 7 + v7 * 8
               + v8 * 9 + v9 * 10 + v10 * 11 + v11 * 12 + v12 * 13 + v13 * 14 + v14 * 15 + v15 * 16;
        long x = (v15 << 0) ^ (v14 << 1) ^ (v13 << 2) ^ (v12 << 3) ^ (v11 << 4) ^ (v10 << 5) ^ (v9 << 6) ^ (v8 << 0)
               ^ (v7 << 1) ^ (v6 << 2) ^ (v5 << 3) ^ (v4 << 4) ^ (v3 << 5) ^ (v2 << 6) ^ (v1 << 0) ^ (v0 << 1);
        return s + x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Live17(ref long p)
    {
        long v0 = Unsafe.Add(ref p, 0); long v1 = Unsafe.Add(ref p, 1); long v2 = Unsafe.Add(ref p, 2);
        long v3 = Unsafe.Add(ref p, 3); long v4 = Unsafe.Add(ref p, 4); long v5 = Unsafe.Add(ref p, 5);
        long v6 = Unsafe.Add(ref p, 6); long v7 = Unsafe.Add(ref p, 7); long v8 = Unsafe.Add(ref p, 8);
        long v9 = Unsafe.Add(ref p, 9); long v10 = Unsafe.Add(ref p, 10); long v11 = Unsafe.Add(ref p, 11);
        long v12 = Unsafe.Add(ref p, 12); long v13 = Unsafe.Add(ref p, 13); long v14 = Unsafe.Add(ref p, 14);
        long v15 = Unsafe.Add(ref p, 15); long v16 = Unsafe.Add(ref p, 16);
        long s = v0 * 1 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6 + v6 * 7 + v7 * 8
               + v8 * 9 + v9 * 10 + v10 * 11 + v11 * 12 + v12 * 13 + v13 * 14 + v14 * 15 + v15 * 16
               + v16 * 17;
        long x = (v16 << 0) ^ (v15 << 1) ^ (v14 << 2) ^ (v13 << 3) ^ (v12 << 4) ^ (v11 << 5) ^ (v10 << 6) ^ (v9 << 0)
               ^ (v8 << 1) ^ (v7 << 2) ^ (v6 << 3) ^ (v5 << 4) ^ (v4 << 5) ^ (v3 << 6) ^ (v2 << 0) ^ (v1 << 1)
               ^ (v0 << 2);
        return s + x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Live18(ref long p)
    {
        long v0 = Unsafe.Add(ref p, 0); long v1 = Unsafe.Add(ref p, 1); long v2 = Unsafe.Add(ref p, 2);
        long v3 = Unsafe.Add(ref p, 3); long v4 = Unsafe.Add(ref p, 4); long v5 = Unsafe.Add(ref p, 5);
        long v6 = Unsafe.Add(ref p, 6); long v7 = Unsafe.Add(ref p, 7); long v8 = Unsafe.Add(ref p, 8);
        long v9 = Unsafe.Add(ref p, 9); long v10 = Unsafe.Add(ref p, 10); long v11 = Unsafe.Add(ref p, 11);
        long v12 = Unsafe.Add(ref p, 12); long v13 = Unsafe.Add(ref p, 13); long v14 = Unsafe.Add(ref p, 14);
        long v15 = Unsafe.Add(ref p, 15); long v16 = Unsafe.Add(ref p, 16); long v17 = Unsafe.Add(ref p, 17);
        long s = v0 * 1 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6 + v6 * 7 + v7 * 8
               + v8 * 9 + v9 * 10 + v10 * 11 + v11 * 12 + v12 * 13 + v13 * 14 + v14 * 15 + v15 * 16
               + v16 * 17 + v17 * 18;
        long x = (v17 << 0) ^ (v16 << 1) ^ (v15 << 2) ^ (v14 << 3) ^ (v13 << 4) ^ (v12 << 5) ^ (v11 << 6) ^ (v10 << 0)
               ^ (v9 << 1) ^ (v8 << 2) ^ (v7 << 3) ^ (v6 << 4) ^ (v5 << 5) ^ (v4 << 6) ^ (v3 << 0) ^ (v2 << 1)
               ^ (v1 << 2) ^ (v0 << 3);
        return s + x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Live19(ref long p)
    {
        long v0 = Unsafe.Add(ref p, 0); long v1 = Unsafe.Add(ref p, 1); long v2 = Unsafe.Add(ref p, 2);
        long v3 = Unsafe.Add(ref p, 3); long v4 = Unsafe.Add(ref p, 4); long v5 = Unsafe.Add(ref p, 5);
        long v6 = Unsafe.Add(ref p, 6); long v7 = Unsafe.Add(ref p, 7); long v8 = Unsafe.Add(ref p, 8);
        long v9 = Unsafe.Add(ref p, 9); long v10 = Unsafe.Add(ref p, 10); long v11 = Unsafe.Add(ref p, 11);
        long v12 = Unsafe.Add(ref p, 12); long v13 = Unsafe.Add(ref p, 13); long v14 = Unsafe.Add(ref p, 14);
        long v15 = Unsafe.Add(ref p, 15); long v16 = Unsafe.Add(ref p, 16); long v17 = Unsafe.Add(ref p, 17);
        long v18 = Unsafe.Add(ref p, 18);
        long s = v0 * 1 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6 + v6 * 7 + v7 * 8
               + v8 * 9 + v9 * 10 + v10 * 11 + v11 * 12 + v12 * 13 + v13 * 14 + v14 * 15 + v15 * 16
               + v16 * 17 + v17 * 18 + v18 * 19;
        long x = (v18 << 0) ^ (v17 << 1) ^ (v16 << 2) ^ (v15 << 3) ^ (v14 << 4) ^ (v13 << 5) ^ (v12 << 6) ^ (v11 << 0)
               ^ (v10 << 1) ^ (v9 << 2) ^ (v8 << 3) ^ (v7 << 4) ^ (v6 << 5) ^ (v5 << 6) ^ (v4 << 0) ^ (v3 << 1)
               ^ (v2 << 2) ^ (v1 << 3) ^ (v0 << 4);
        return s + x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Live20(ref long p)
    {
        long v0 = Unsafe.Add(ref p, 0); long v1 = Unsafe.Add(ref p, 1); long v2 = Unsafe.Add(ref p, 2);
        long v3 = Unsafe.Add(ref p, 3); long v4 = Unsafe.Add(ref p, 4); long v5 = Unsafe.Add(ref p, 5);
        long v6 = Unsafe.Add(ref p, 6); long v7 = Unsafe.Add(ref p, 7); long v8 = Unsafe.Add(ref p, 8);
        long v9 = Unsafe.Add(ref p, 9); long v10 = Unsafe.Add(ref p, 10); long v11 = Unsafe.Add(ref p, 11);
        long v12 = Unsafe.Add(ref p, 12); long v13 = Unsafe.Add(ref p, 13); long v14 = Unsafe.Add(ref p, 14);
        long v15 = Unsafe.Add(ref p, 15); long v16 = Unsafe.Add(ref p, 16); long v17 = Unsafe.Add(ref p, 17);
        long v18 = Unsafe.Add(ref p, 18); long v19 = Unsafe.Add(ref p, 19);
        long s = v0 * 1 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6 + v6 * 7 + v7 * 8
               + v8 * 9 + v9 * 10 + v10 * 11 + v11 * 12 + v12 * 13 + v13 * 14 + v14 * 15 + v15 * 16
               + v16 * 17 + v17 * 18 + v18 * 19 + v19 * 20;
        long x = (v19 << 0) ^ (v18 << 1) ^ (v17 << 2) ^ (v16 << 3) ^ (v15 << 4) ^ (v14 << 5) ^ (v13 << 6) ^ (v12 << 0)
               ^ (v11 << 1) ^ (v10 << 2) ^ (v9 << 3) ^ (v8 << 4) ^ (v7 << 5) ^ (v6 << 6) ^ (v5 << 0) ^ (v4 << 1)
               ^ (v3 << 2) ^ (v2 << 3) ^ (v1 << 4) ^ (v0 << 5);
        return s + x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Live21(ref long p)
    {
        long v0 = Unsafe.Add(ref p, 0); long v1 = Unsafe.Add(ref p, 1); long v2 = Unsafe.Add(ref p, 2);
        long v3 = Unsafe.Add(ref p, 3); long v4 = Unsafe.Add(ref p, 4); long v5 = Unsafe.Add(ref p, 5);
        long v6 = Unsafe.Add(ref p, 6); long v7 = Unsafe.Add(ref p, 7); long v8 = Unsafe.Add(ref p, 8);
        long v9 = Unsafe.Add(ref p, 9); long v10 = Unsafe.Add(ref p, 10); long v11 = Unsafe.Add(ref p, 11);
        long v12 = Unsafe.Add(ref p, 12); long v13 = Unsafe.Add(ref p, 13); long v14 = Unsafe.Add(ref p, 14);
        long v15 = Unsafe.Add(ref p, 15); long v16 = Unsafe.Add(ref p, 16); long v17 = Unsafe.Add(ref p, 17);
        long v18 = Unsafe.Add(ref p, 18); long v19 = Unsafe.Add(ref p, 19); long v20 = Unsafe.Add(ref p, 20);
        long s = v0 * 1 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6 + v6 * 7 + v7 * 8
               + v8 * 9 + v9 * 10 + v10 * 11 + v11 * 12 + v12 * 13 + v13 * 14 + v14 * 15 + v15 * 16
               + v16 * 17 + v17 * 18 + v18 * 19 + v19 * 20 + v20 * 21;
        long x = (v20 << 0) ^ (v19 << 1) ^ (v18 << 2) ^ (v17 << 3) ^ (v16 << 4) ^ (v15 << 5) ^ (v14 << 6) ^ (v13 << 0)
               ^ (v12 << 1) ^ (v11 << 2) ^ (v10 << 3) ^ (v9 << 4) ^ (v8 << 5) ^ (v7 << 6) ^ (v6 << 0) ^ (v5 << 1)
               ^ (v4 << 2) ^ (v3 << 3) ^ (v2 << 4) ^ (v1 << 5) ^ (v0 << 6);
        return s + x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Live22(ref long p)
    {
        long v0 = Unsafe.Add(ref p, 0); long v1 = Unsafe.Add(ref p, 1); long v2 = Unsafe.Add(ref p, 2);
        long v3 = Unsafe.Add(ref p, 3); long v4 = Unsafe.Add(ref p, 4); long v5 = Unsafe.Add(ref p, 5);
        long v6 = Unsafe.Add(ref p, 6); long v7 = Unsafe.Add(ref p, 7); long v8 = Unsafe.Add(ref p, 8);
        long v9 = Unsafe.Add(ref p, 9); long v10 = Unsafe.Add(ref p, 10); long v11 = Unsafe.Add(ref p, 11);
        long v12 = Unsafe.Add(ref p, 12); long v13 = Unsafe.Add(ref p, 13); long v14 = Unsafe.Add(ref p, 14);
        long v15 = Unsafe.Add(ref p, 15); long v16 = Unsafe.Add(ref p, 16); long v17 = Unsafe.Add(ref p, 17);
        long v18 = Unsafe.Add(ref p, 18); long v19 = Unsafe.Add(ref p, 19); long v20 = Unsafe.Add(ref p, 20);
        long v21 = Unsafe.Add(ref p, 21);
        long s = v0 * 1 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6 + v6 * 7 + v7 * 8
               + v8 * 9 + v9 * 10 + v10 * 11 + v11 * 12 + v12 * 13 + v13 * 14 + v14 * 15 + v15 * 16
               + v16 * 17 + v17 * 18 + v18 * 19 + v19 * 20 + v20 * 21 + v21 * 22;
        long x = (v21 << 0) ^ (v20 << 1) ^ (v19 << 2) ^ (v18 << 3) ^ (v17 << 4) ^ (v16 << 5) ^ (v15 << 6) ^ (v14 << 0)
               ^ (v13 << 1) ^ (v12 << 2) ^ (v11 << 3) ^ (v10 << 4) ^ (v9 << 5) ^ (v8 << 6) ^ (v7 << 0) ^ (v6 << 1)
               ^ (v5 << 2) ^ (v4 << 3) ^ (v3 << 4) ^ (v2 << 5) ^ (v1 << 6) ^ (v0 << 0);
        return s + x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Live23(ref long p)
    {
        long v0 = Unsafe.Add(ref p, 0); long v1 = Unsafe.Add(ref p, 1); long v2 = Unsafe.Add(ref p, 2);
        long v3 = Unsafe.Add(ref p, 3); long v4 = Unsafe.Add(ref p, 4); long v5 = Unsafe.Add(ref p, 5);
        long v6 = Unsafe.Add(ref p, 6); long v7 = Unsafe.Add(ref p, 7); long v8 = Unsafe.Add(ref p, 8);
        long v9 = Unsafe.Add(ref p, 9); long v10 = Unsafe.Add(ref p, 10); long v11 = Unsafe.Add(ref p, 11);
        long v12 = Unsafe.Add(ref p, 12); long v13 = Unsafe.Add(ref p, 13); long v14 = Unsafe.Add(ref p, 14);
        long v15 = Unsafe.Add(ref p, 15); long v16 = Unsafe.Add(ref p, 16); long v17 = Unsafe.Add(ref p, 17);
        long v18 = Unsafe.Add(ref p, 18); long v19 = Unsafe.Add(ref p, 19); long v20 = Unsafe.Add(ref p, 20);
        long v21 = Unsafe.Add(ref p, 21); long v22 = Unsafe.Add(ref p, 22);
        long s = v0 * 1 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6 + v6 * 7 + v7 * 8
               + v8 * 9 + v9 * 10 + v10 * 11 + v11 * 12 + v12 * 13 + v13 * 14 + v14 * 15 + v15 * 16
               + v16 * 17 + v17 * 18 + v18 * 19 + v19 * 20 + v20 * 21 + v21 * 22 + v22 * 23;
        long x = (v22 << 0) ^ (v21 << 1) ^ (v20 << 2) ^ (v19 << 3) ^ (v18 << 4) ^ (v17 << 5) ^ (v16 << 6) ^ (v15 << 0)
               ^ (v14 << 1) ^ (v13 << 2) ^ (v12 << 3) ^ (v11 << 4) ^ (v10 << 5) ^ (v9 << 6) ^ (v8 << 0) ^ (v7 << 1)
               ^ (v6 << 2) ^ (v5 << 3) ^ (v4 << 4) ^ (v3 << 5) ^ (v2 << 6) ^ (v1 << 0) ^ (v0 << 1);
        return s + x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Live24(ref long p)
    {
        long v0 = Unsafe.Add(ref p, 0); long v1 = Unsafe.Add(ref p, 1); long v2 = Unsafe.Add(ref p, 2);
        long v3 = Unsafe.Add(ref p, 3); long v4 = Unsafe.Add(ref p, 4); long v5 = Unsafe.Add(ref p, 5);
        long v6 = Unsafe.Add(ref p, 6); long v7 = Unsafe.Add(ref p, 7); long v8 = Unsafe.Add(ref p, 8);
        long v9 = Unsafe.Add(ref p, 9); long v10 = Unsafe.Add(ref p, 10); long v11 = Unsafe.Add(ref p, 11);
        long v12 = Unsafe.Add(ref p, 12); long v13 = Unsafe.Add(ref p, 13); long v14 = Unsafe.Add(ref p, 14);
        long v15 = Unsafe.Add(ref p, 15); long v16 = Unsafe.Add(ref p, 16); long v17 = Unsafe.Add(ref p, 17);
        long v18 = Unsafe.Add(ref p, 18); long v19 = Unsafe.Add(ref p, 19); long v20 = Unsafe.Add(ref p, 20);
        long v21 = Unsafe.Add(ref p, 21); long v22 = Unsafe.Add(ref p, 22); long v23 = Unsafe.Add(ref p, 23);
        long s = v0 * 1 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6 + v6 * 7 + v7 * 8
               + v8 * 9 + v9 * 10 + v10 * 11 + v11 * 12 + v12 * 13 + v13 * 14 + v14 * 15 + v15 * 16
               + v16 * 17 + v17 * 18 + v18 * 19 + v19 * 20 + v20 * 21 + v21 * 22 + v22 * 23 + v23 * 24;
        long x = (v23 << 0) ^ (v22 << 1) ^ (v21 << 2) ^ (v20 << 3) ^ (v19 << 4) ^ (v18 << 5) ^ (v17 << 6) ^ (v16 << 0)
               ^ (v15 << 1) ^ (v14 << 2) ^ (v13 << 3) ^ (v12 << 4) ^ (v11 << 5) ^ (v10 << 6) ^ (v9 << 0) ^ (v8 << 1)
               ^ (v7 << 2) ^ (v6 << 3) ^ (v5 << 4) ^ (v4 << 5) ^ (v3 << 6) ^ (v2 << 0) ^ (v1 << 1) ^ (v0 << 2);
        return s + x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Live25(ref long p)
    {
        long v0 = Unsafe.Add(ref p, 0); long v1 = Unsafe.Add(ref p, 1); long v2 = Unsafe.Add(ref p, 2);
        long v3 = Unsafe.Add(ref p, 3); long v4 = Unsafe.Add(ref p, 4); long v5 = Unsafe.Add(ref p, 5);
        long v6 = Unsafe.Add(ref p, 6); long v7 = Unsafe.Add(ref p, 7); long v8 = Unsafe.Add(ref p, 8);
        long v9 = Unsafe.Add(ref p, 9); long v10 = Unsafe.Add(ref p, 10); long v11 = Unsafe.Add(ref p, 11);
        long v12 = Unsafe.Add(ref p, 12); long v13 = Unsafe.Add(ref p, 13); long v14 = Unsafe.Add(ref p, 14);
        long v15 = Unsafe.Add(ref p, 15); long v16 = Unsafe.Add(ref p, 16); long v17 = Unsafe.Add(ref p, 17);
        long v18 = Unsafe.Add(ref p, 18); long v19 = Unsafe.Add(ref p, 19); long v20 = Unsafe.Add(ref p, 20);
        long v21 = Unsafe.Add(ref p, 21); long v22 = Unsafe.Add(ref p, 22); long v23 = Unsafe.Add(ref p, 23);
        long v24 = Unsafe.Add(ref p, 24);
        long s = v0 * 1 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6 + v6 * 7 + v7 * 8
               + v8 * 9 + v9 * 10 + v10 * 11 + v11 * 12 + v12 * 13 + v13 * 14 + v14 * 15 + v15 * 16
               + v16 * 17 + v17 * 18 + v18 * 19 + v19 * 20 + v20 * 21 + v21 * 22 + v22 * 23 + v23 * 24
               + v24 * 25;
        long x = (v24 << 0) ^ (v23 << 1) ^ (v22 << 2) ^ (v21 << 3) ^ (v20 << 4) ^ (v19 << 5) ^ (v18 << 6) ^ (v17 << 0)
               ^ (v16 << 1) ^ (v15 << 2) ^ (v14 << 3) ^ (v13 << 4) ^ (v12 << 5) ^ (v11 << 6) ^ (v10 << 0) ^ (v9 << 1)
               ^ (v8 << 2) ^ (v7 << 3) ^ (v6 << 4) ^ (v5 << 5) ^ (v4 << 6) ^ (v3 << 0) ^ (v2 << 1) ^ (v1 << 2)
               ^ (v0 << 3);
        return s + x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Live26(ref long p)
    {
        long v0 = Unsafe.Add(ref p, 0); long v1 = Unsafe.Add(ref p, 1); long v2 = Unsafe.Add(ref p, 2);
        long v3 = Unsafe.Add(ref p, 3); long v4 = Unsafe.Add(ref p, 4); long v5 = Unsafe.Add(ref p, 5);
        long v6 = Unsafe.Add(ref p, 6); long v7 = Unsafe.Add(ref p, 7); long v8 = Unsafe.Add(ref p, 8);
        long v9 = Unsafe.Add(ref p, 9); long v10 = Unsafe.Add(ref p, 10); long v11 = Unsafe.Add(ref p, 11);
        long v12 = Unsafe.Add(ref p, 12); long v13 = Unsafe.Add(ref p, 13); long v14 = Unsafe.Add(ref p, 14);
        long v15 = Unsafe.Add(ref p, 15); long v16 = Unsafe.Add(ref p, 16); long v17 = Unsafe.Add(ref p, 17);
        long v18 = Unsafe.Add(ref p, 18); long v19 = Unsafe.Add(ref p, 19); long v20 = Unsafe.Add(ref p, 20);
        long v21 = Unsafe.Add(ref p, 21); long v22 = Unsafe.Add(ref p, 22); long v23 = Unsafe.Add(ref p, 23);
        long v24 = Unsafe.Add(ref p, 24); long v25 = Unsafe.Add(ref p, 25);
        long s = v0 * 1 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6 + v6 * 7 + v7 * 8
               + v8 * 9 + v9 * 10 + v10 * 11 + v11 * 12 + v12 * 13 + v13 * 14 + v14 * 15 + v15 * 16
               + v16 * 17 + v17 * 18 + v18 * 19 + v19 * 20 + v20 * 21 + v21 * 22 + v22 * 23 + v23 * 24
               + v24 * 25 + v25 * 26;
        long x = (v25 << 0) ^ (v24 << 1) ^ (v23 << 2) ^ (v22 << 3) ^ (v21 << 4) ^ (v20 << 5) ^ (v19 << 6) ^ (v18 << 0)
               ^ (v17 << 1) ^ (v16 << 2) ^ (v15 << 3) ^ (v14 << 4) ^ (v13 << 5) ^ (v12 << 6) ^ (v11 << 0) ^ (v10 << 1)
               ^ (v9 << 2) ^ (v8 << 3) ^ (v7 << 4) ^ (v6 << 5) ^ (v5 << 6) ^ (v4 << 0) ^ (v3 << 1) ^ (v2 << 2)
               ^ (v1 << 3) ^ (v0 << 4);
        return s + x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Live30(ref long p)
    {
        long v0 = Unsafe.Add(ref p, 0); long v1 = Unsafe.Add(ref p, 1); long v2 = Unsafe.Add(ref p, 2);
        long v3 = Unsafe.Add(ref p, 3); long v4 = Unsafe.Add(ref p, 4); long v5 = Unsafe.Add(ref p, 5);
        long v6 = Unsafe.Add(ref p, 6); long v7 = Unsafe.Add(ref p, 7); long v8 = Unsafe.Add(ref p, 8);
        long v9 = Unsafe.Add(ref p, 9); long v10 = Unsafe.Add(ref p, 10); long v11 = Unsafe.Add(ref p, 11);
        long v12 = Unsafe.Add(ref p, 12); long v13 = Unsafe.Add(ref p, 13); long v14 = Unsafe.Add(ref p, 14);
        long v15 = Unsafe.Add(ref p, 15); long v16 = Unsafe.Add(ref p, 16); long v17 = Unsafe.Add(ref p, 17);
        long v18 = Unsafe.Add(ref p, 18); long v19 = Unsafe.Add(ref p, 19); long v20 = Unsafe.Add(ref p, 20);
        long v21 = Unsafe.Add(ref p, 21); long v22 = Unsafe.Add(ref p, 22); long v23 = Unsafe.Add(ref p, 23);
        long v24 = Unsafe.Add(ref p, 24); long v25 = Unsafe.Add(ref p, 25); long v26 = Unsafe.Add(ref p, 26);
        long v27 = Unsafe.Add(ref p, 27); long v28 = Unsafe.Add(ref p, 28); long v29 = Unsafe.Add(ref p, 29);
        long s = v0 * 1 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6 + v6 * 7 + v7 * 8
               + v8 * 9 + v9 * 10 + v10 * 11 + v11 * 12 + v12 * 13 + v13 * 14 + v14 * 15 + v15 * 16
               + v16 * 17 + v17 * 18 + v18 * 19 + v19 * 20 + v20 * 21 + v21 * 22 + v22 * 23 + v23 * 24
               + v24 * 25 + v25 * 26 + v26 * 27 + v27 * 28 + v28 * 29 + v29 * 30;
        long x = (v29 << 0) ^ (v28 << 1) ^ (v27 << 2) ^ (v26 << 3) ^ (v25 << 4) ^ (v24 << 5) ^ (v23 << 6) ^ (v22 << 0)
               ^ (v21 << 1) ^ (v20 << 2) ^ (v19 << 3) ^ (v18 << 4) ^ (v17 << 5) ^ (v16 << 6) ^ (v15 << 0) ^ (v14 << 1)
               ^ (v13 << 2) ^ (v12 << 3) ^ (v11 << 4) ^ (v10 << 5) ^ (v9 << 6) ^ (v8 << 0) ^ (v7 << 1) ^ (v6 << 2)
               ^ (v5 << 3) ^ (v4 << 4) ^ (v3 << 5) ^ (v2 << 6) ^ (v1 << 0) ^ (v0 << 1);
        return s + x;
    }

    // ---- many values alive at once and a buffer in the frame ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long BufLive4<T>(ref long p, long a, long b) where T : struct
    {
        T buf;
        Unsafe.SkipInit(out buf);
        ref long q = ref Unsafe.As<T, long>(ref buf);
        int last = Unsafe.SizeOf<T>() / 8 - 1;
        Volatile.Write(ref q, a);
        Volatile.Write(ref Unsafe.Add(ref q, last), b);
        long v0 = Unsafe.Add(ref p, 0); long v1 = Unsafe.Add(ref p, 1); long v2 = Unsafe.Add(ref p, 2);
        long v3 = Unsafe.Add(ref p, 3);
        long s = v0 * 1 + v1 * 2 + v2 * 3 + v3 * 4;
        long x = (v3 << 0) ^ (v2 << 1) ^ (v1 << 2) ^ (v0 << 3);
        return s + x + Volatile.Read(ref q) * 3 + Volatile.Read(ref Unsafe.Add(ref q, last)) * 5;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long BufLive20<T>(ref long p, long a, long b) where T : struct
    {
        T buf;
        Unsafe.SkipInit(out buf);
        ref long q = ref Unsafe.As<T, long>(ref buf);
        int last = Unsafe.SizeOf<T>() / 8 - 1;
        Volatile.Write(ref q, a);
        Volatile.Write(ref Unsafe.Add(ref q, last), b);
        long v0 = Unsafe.Add(ref p, 0); long v1 = Unsafe.Add(ref p, 1); long v2 = Unsafe.Add(ref p, 2);
        long v3 = Unsafe.Add(ref p, 3); long v4 = Unsafe.Add(ref p, 4); long v5 = Unsafe.Add(ref p, 5);
        long v6 = Unsafe.Add(ref p, 6); long v7 = Unsafe.Add(ref p, 7); long v8 = Unsafe.Add(ref p, 8);
        long v9 = Unsafe.Add(ref p, 9); long v10 = Unsafe.Add(ref p, 10); long v11 = Unsafe.Add(ref p, 11);
        long v12 = Unsafe.Add(ref p, 12); long v13 = Unsafe.Add(ref p, 13); long v14 = Unsafe.Add(ref p, 14);
        long v15 = Unsafe.Add(ref p, 15); long v16 = Unsafe.Add(ref p, 16); long v17 = Unsafe.Add(ref p, 17);
        long v18 = Unsafe.Add(ref p, 18); long v19 = Unsafe.Add(ref p, 19);
        long s = v0 * 1 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6 + v6 * 7 + v7 * 8
               + v8 * 9 + v9 * 10 + v10 * 11 + v11 * 12 + v12 * 13 + v13 * 14 + v14 * 15 + v15 * 16
               + v16 * 17 + v17 * 18 + v18 * 19 + v19 * 20;
        long x = (v19 << 0) ^ (v18 << 1) ^ (v17 << 2) ^ (v16 << 3) ^ (v15 << 4) ^ (v14 << 5) ^ (v13 << 6) ^ (v12 << 0)
               ^ (v11 << 1) ^ (v10 << 2) ^ (v9 << 3) ^ (v8 << 4) ^ (v7 << 5) ^ (v6 << 6) ^ (v5 << 0) ^ (v4 << 1)
               ^ (v3 << 2) ^ (v2 << 3) ^ (v1 << 4) ^ (v0 << 5);
        return s + x + Volatile.Read(ref q) * 3 + Volatile.Read(ref Unsafe.Add(ref q, last)) * 5;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long BufLive27<T>(ref long p, long a, long b) where T : struct
    {
        T buf;
        Unsafe.SkipInit(out buf);
        ref long q = ref Unsafe.As<T, long>(ref buf);
        int last = Unsafe.SizeOf<T>() / 8 - 1;
        Volatile.Write(ref q, a);
        Volatile.Write(ref Unsafe.Add(ref q, last), b);
        long v0 = Unsafe.Add(ref p, 0); long v1 = Unsafe.Add(ref p, 1); long v2 = Unsafe.Add(ref p, 2);
        long v3 = Unsafe.Add(ref p, 3); long v4 = Unsafe.Add(ref p, 4); long v5 = Unsafe.Add(ref p, 5);
        long v6 = Unsafe.Add(ref p, 6); long v7 = Unsafe.Add(ref p, 7); long v8 = Unsafe.Add(ref p, 8);
        long v9 = Unsafe.Add(ref p, 9); long v10 = Unsafe.Add(ref p, 10); long v11 = Unsafe.Add(ref p, 11);
        long v12 = Unsafe.Add(ref p, 12); long v13 = Unsafe.Add(ref p, 13); long v14 = Unsafe.Add(ref p, 14);
        long v15 = Unsafe.Add(ref p, 15); long v16 = Unsafe.Add(ref p, 16); long v17 = Unsafe.Add(ref p, 17);
        long v18 = Unsafe.Add(ref p, 18); long v19 = Unsafe.Add(ref p, 19); long v20 = Unsafe.Add(ref p, 20);
        long v21 = Unsafe.Add(ref p, 21); long v22 = Unsafe.Add(ref p, 22); long v23 = Unsafe.Add(ref p, 23);
        long v24 = Unsafe.Add(ref p, 24); long v25 = Unsafe.Add(ref p, 25); long v26 = Unsafe.Add(ref p, 26);
        long s = v0 * 1 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6 + v6 * 7 + v7 * 8
               + v8 * 9 + v9 * 10 + v10 * 11 + v11 * 12 + v12 * 13 + v13 * 14 + v14 * 15 + v15 * 16
               + v16 * 17 + v17 * 18 + v18 * 19 + v19 * 20 + v20 * 21 + v21 * 22 + v22 * 23 + v23 * 24
               + v24 * 25 + v25 * 26 + v26 * 27;
        long x = (v26 << 0) ^ (v25 << 1) ^ (v24 << 2) ^ (v23 << 3) ^ (v22 << 4) ^ (v21 << 5) ^ (v20 << 6) ^ (v19 << 0)
               ^ (v18 << 1) ^ (v17 << 2) ^ (v16 << 3) ^ (v15 << 4) ^ (v14 << 5) ^ (v13 << 6) ^ (v12 << 0) ^ (v11 << 1)
               ^ (v10 << 2) ^ (v9 << 3) ^ (v8 << 4) ^ (v7 << 5) ^ (v6 << 6) ^ (v5 << 0) ^ (v4 << 1) ^ (v3 << 2)
               ^ (v2 << 3) ^ (v1 << 4) ^ (v0 << 5);
        return s + x + Volatile.Read(ref q) * 3 + Volatile.Read(ref Unsafe.Add(ref q, last)) * 5;
    }

    // ---- the groups ----

    static int Plain()
    {
        int f = 0;
        f += Eq("arith", Arith(100, 7), ((100 + 21 - 25) ^ (7 << 5)) | 100);
        f += Eq("arith32", Arith32(-100, 7), (-79 ^ -13) ^ 1);
        f += Eq("shift", ShiftVar(-256, 4), -4096 - 16 + 0x0ffffffffffffff0);
        f += Eq("cmp signed <", CmpSigned(-5, 3), 1 | 2 | 32);
        f += Eq("cmp signed >", CmpSigned(3, -5), 4 | 8 | 32);
        f += Eq("cmp signed =", CmpSigned(long.MinValue, long.MinValue), 2 | 8 | 16);
        f += Eq("cmp signed min", CmpSigned(long.MinValue, long.MaxValue), 1 | 2 | 32);
        f += Eq("cmp unsigned <", CmpUnsigned(3, ulong.MaxValue), 1 | 2);
        f += Eq("cmp unsigned >", CmpUnsigned(0x8000000000000000, 5), 4 | 8);
        f += Eq("cmp unsigned =", CmpUnsigned(7, 7), 2 | 8);
        f += Eq("cmp int <", CmpInt(int.MinValue, 1), 1 | 2);
        f += Eq("cmp int >", CmpInt(1, -1), 4 | 8);
        f += Eq("cmp uint <", CmpUInt(1, 0xffffffff), 1 | 2);
        f += Eq("cmp uint >", CmpUInt(0x80000000, 0x7fffffff), 4 | 8);
        f += Eq("cmp low half 1", CmpLowHalf(0x1_ffffffff, 0x2_00000000), 1);
        f += Eq("cmp low half 2", CmpLowHalf(0x7_80000001, 0x1_80000000), 2);
        f += Eq("cmp low half 3", CmpLowHalf(0x5_00000007, 0x9_00000007), 3);
        f += Eq("cmp const small", CmpBigConst(5), 1 | 2 | 8 | 16 | 32);
        f += Eq("cmp const 2048", CmpBigConst(2048), 1 | 2 | 32);
        f += Eq("cmp const big", CmpBigConst(0x123456789AB), 2 | 4 | 32);
        f += Eq("cmp const low", CmpBigConst(-5000000000), 1 | 8 | 16);
        f += Eq("cmp const -2049", CmpBigConst(-2049), 1 | 2 | 8 | 16);
        f += Eq("cmp const int", CmpBigConstInt(123456), 1 | 2);
        f += Eq("cmp const int neg", CmpBigConstInt(-1000001), 1 | 4 | 8);
        f += Eq("branches 1", Branches(-3, 9), -1);
        f += Eq("branches 2", Branches(4, 9), 10);
        f += Eq("branches 3", Branches(99999, 9), 18);
        f += Eq("branches 4", Branches(100000, 100009), -9);
        f += Eq("branches 5", Branches(100000, 15), 100000 ^ 15);
        f += Eq("select", Select(-4, 9), 27 - 4 + 13);
        return f;
    }

    static int Multiply()
    {
        int f = 0;
        f += Eq("div const", DivConst(1234567), 176366 + 567 + 123456 + 411522 + 246913);
        f += Eq("div const neg", DivConst(-15), -2 - 15 + 1844674407370955160 - 5 + 858993456);
        f += Eq("mul high", MulHigh(0x100000000, 0x300000005), 3 + 0x500000000);
        f += Eq("mul high max", MulHigh(ulong.MaxValue, ulong.MaxValue), -2 + 1);
        f += Eq("mul high signed", MulHighSigned(-3, 5), -1000 - 15);
        f += Eq("mul high signed big", MulHighSigned(long.MinValue, 4), -2000);
        f += Eq("mul wide", MulWide(-2, 3), -6 + 12884901882);
        for (int i = 0; i < 10; i++)
            f += Eq("switch " + i, Switch(i, 40), i switch { 0 => 41, 1 => 120, 2 => 40 ^ 0x55, 3 => 10, 4 => -40,
                5 => 320, 6 => 31, 7 => 296, _ => 0 });
        f += Eq("switch neg", Switch(-1, 40), 0);
        return f;
    }

    static int Bits()
    {
        int f = 0;
        f += Eq("rotate", RotateOnly(0xF000000000000001), 0x1F);
        f += Eq("rotates", Rotates(0x8000000000000001, 1), 0x3 ^ 0x3000000000000000 ^ 0x30000);
        f += Eq("rotates by 0", Rotates(0x8000000000000001, 0),
                unchecked((long)(0x8000000000000001 ^ 0x3000000000000000 ^ 0x30000)));
        f += Eq("rotates32", Rotates32(0x80000001, 4), 0x18 ^ 0x30000000 ^ 0x18000000);
        f += Eq("bit counts", BitCounts(0x00F0000000001000), 5 * 10000 + 8 * 100 + 12);
        f += Eq("bit counts one", BitCounts(1), 10000 + 6300);
        f += Eq("swaps", Swaps(0x0102030405060708), 0x0807060504030201 + 0x08070605);
        long cell = 10;
        f += Eq("interlocked", Locked(ref cell, 4), 14 + 14 + 8 + 12 + 12);
        f += Eq("interlocked cell", cell, 12);
        int cell32 = 6;
        f += Eq("interlocked32", Locked32(ref cell32, 20), 7 + 7 + 20 + 25);
        return f;
    }

    static int Helpers()
    {
        int f = 0;
        f += Eq("checked add", CheckedAdd(long.MaxValue - 5, 5), long.MaxValue);
        f += Eq("checked mul", CheckedMul(-3000000000, 3000000000), -9000000000000000000);
        f += Eq("checked mul32", CheckedMul32(-46340, 46340), -2147395600);
        f += Eq("checked mul u32", CheckedMulU32(65535, 65537), 4294967295);
        f += Eq("checked narrow", CheckedNarrow(-2147483648), -2147483648);
        int thrown = 0;
        try { CheckedAdd(long.MaxValue, 1); } catch (OverflowException) { thrown++; }
        try { CheckedMul(long.MaxValue, 2); } catch (OverflowException) { thrown++; }
        try { CheckedMul32(46341, 46341); } catch (OverflowException) { thrown++; }
        try { CheckedMulU32(65536, 65536); } catch (OverflowException) { thrown++; }
        try { CheckedNarrow(2147483648); } catch (OverflowException) { thrown++; }
        try { DivVar(5, 0); } catch (DivideByZeroException) { thrown++; }
        long[] arr = { 5, 6, 7 };
        try { Index(arr, 2); } catch (IndexOutOfRangeException) { thrown++; }
        f += Eq("throws", thrown, 7);
        f += Eq("div var", DivVar(-47, 5), -9 - 2);
        f += Eq("index", Index(arr, 1), 13);
        f += Eq("loop", Loop(100), 5050);
        f += Eq("read static", ReadStatic(2), 42);

        S16 a16 = new S16 { A = 1, B = 2 }, b16 = default;
        Copy16(ref b16, ref a16);
        f += Eq("copy 16", b16.A * 10 + b16.B, 12);
        S24 a24 = new S24 { A = 1, B = 2, C = 3 }, b24 = default;
        Copy24(ref b24, ref a24);
        f += Eq("copy 24", b24.A * 100 + b24.B * 10 + b24.C, 123);
        S32 a32 = new S32 { A = 1, B = 2, C = 3, D = 4 }, b32 = default;
        Copy32(ref b32, ref a32);
        f += Eq("copy 32", b32.A * 1000 + b32.B * 100 + b32.C * 10 + b32.D, 1234);
        S40 a40 = new S40 { A = 1, B = 2, C = 3, D = 4, E = 5 }, b40 = default;
        Copy40(ref b40, ref a40);
        f += Eq("copy 40", b40.A * 10000 + b40.B * 1000 + b40.C * 100 + b40.D * 10 + b40.E, 12345);
        S64 a64 = new S64 { A = 1, B = 2, C = 3, D = 4, E = 5, F = 6, G = 7, H = 8 }, b64 = default;
        Copy64(ref b64, ref a64);
        f += Eq("copy 64", b64.A + b64.B * 2 + b64.C * 3 + b64.D * 4 + b64.E * 5 + b64.F * 6 + b64.G * 7 + b64.H * 8,
                204);
        Clear64(ref b64);
        f += Eq("clear 64", b64.A | b64.B | b64.C | b64.D | b64.E | b64.F | b64.G | b64.H, 0);

        object o1 = "one", o2 = "two";
        SRef sa = new SRef { O = o1, L = 77, S = "s" }, sb = default;
        CopyRef(ref sb, ref sa);
        f += Eq("copy with references", (ReferenceEquals(sb.O, o1) ? 1 : 0) + (sb.S == "s" ? 2 : 0) + sb.L, 80);
        StoreStatic(o2);
        f += Eq("store static", ReferenceEquals(staticRef, o2) ? 1 : 0, 1);
        object[] objs = new object[3];
        StoreElement(objs, o1);
        f += Eq("store element", ReferenceEquals(objs[1], o1) ? 1 : 0, 1);
        Holder h = new Holder();
        h.SetRef(o2);
        h.Set(803);
        f += Eq("instance", (ReferenceEquals(h.O, o2) ? 1000 : 0) + h.Get(), 1000 + 803 + 100);
        return f;
    }

    static int Values()
    {
        int f = 0;
        S16 p2 = Pair(10, 20);
        f += Eq("pair", p2.A * 100 + p2.B, 1122);
        S40 p5 = Five(9, 4);
        f += Eq("five", p5.A + p5.B * 10 + p5.C * 100 + p5.D * 1000 + p5.E * 10000, 9 + 40 + 1300 + 5000 + 360000);
        f += Eq("stack args", StackArgs(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), 204 + 900 + 10000 + 110000);
        f += Eq("stack args 16", StackArgsBuf<B16>(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), 121104);
        f += Eq("stack args 2032", StackArgsBuf<B2032>(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), 121104);
        f += Eq("stack args 2040", StackArgsBuf<B2040>(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), 121104);
        f += Eq("stack args 2048", StackArgsBuf<B2048>(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), 121104);
        f += Eq("stack args 4096", StackArgsBuf<B4096>(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), 121104);
        return f;
    }

    static readonly long[] words = new long[32];

    static int Live()
    {
        int f = 0;
        ref long p = ref words[0];
        f += Eq("live 13", Live13(ref p), 880);
        f += Eq("live 14", Live14(ref p), 2026);
        f += Eq("live 15", Live15(ref p), 2104);
        f += Eq("live 16", Live16(ref p), 2355);
        f += Eq("live 17", Live17(ref p), 2343);
        f += Eq("live 18", Live18(ref p), 3070);
        f += Eq("live 19", Live19(ref p), 3262);
        f += Eq("live 20", Live20(ref p), 3813);
        f += Eq("live 21", Live21(ref p), 4273);
        f += Eq("live 22", Live22(ref p), 4967);
        f += Eq("live 23", Live23(ref p), 5512);
        f += Eq("live 24", Live24(ref p), 6252);
        f += Eq("live 25", Live25(ref p), 6569);
        f += Eq("live 26", Live26(ref p), 7521);
        f += Eq("live 30", Live30(ref p), 9623);
        return f;
    }

    static int Buffers()
    {
        int f = 0;
        ref long p = ref words[0];
        f += Eq("buffer 16", BufLive4<B16>(ref p, 7, 9) + BufLive20<B16>(ref p, 7, 9) + BufLive27<B16>(ref p, 7, 9),
                12465L);
        f += Eq("buffer 1024",
                BufLive4<B1024>(ref p, 7, 9) +
                BufLive20<B1024>(ref p, 7, 9) + BufLive27<B1024>(ref p, 7, 9),
                12465L);
        f += Eq("buffer 1992",
                BufLive4<B1992>(ref p, 7, 9) +
                BufLive20<B1992>(ref p, 7, 9) + BufLive27<B1992>(ref p, 7, 9),
                12465L);
        f += Eq("buffer 2000",
                BufLive4<B2000>(ref p, 7, 9) +
                BufLive20<B2000>(ref p, 7, 9) + BufLive27<B2000>(ref p, 7, 9),
                12465L);
        f += Eq("buffer 2008",
                BufLive4<B2008>(ref p, 7, 9) +
                BufLive20<B2008>(ref p, 7, 9) + BufLive27<B2008>(ref p, 7, 9),
                12465L);
        f += Eq("buffer 2016",
                BufLive4<B2016>(ref p, 7, 9) +
                BufLive20<B2016>(ref p, 7, 9) + BufLive27<B2016>(ref p, 7, 9),
                12465L);
        f += Eq("buffer 2024",
                BufLive4<B2024>(ref p, 7, 9) +
                BufLive20<B2024>(ref p, 7, 9) + BufLive27<B2024>(ref p, 7, 9),
                12465L);
        f += Eq("buffer 2032",
                BufLive4<B2032>(ref p, 7, 9) +
                BufLive20<B2032>(ref p, 7, 9) + BufLive27<B2032>(ref p, 7, 9),
                12465L);
        f += Eq("buffer 2040",
                BufLive4<B2040>(ref p, 7, 9) +
                BufLive20<B2040>(ref p, 7, 9) + BufLive27<B2040>(ref p, 7, 9),
                12465L);
        f += Eq("buffer 2048",
                BufLive4<B2048>(ref p, 7, 9) +
                BufLive20<B2048>(ref p, 7, 9) + BufLive27<B2048>(ref p, 7, 9),
                12465L);
        f += Eq("buffer 2056",
                BufLive4<B2056>(ref p, 7, 9) +
                BufLive20<B2056>(ref p, 7, 9) + BufLive27<B2056>(ref p, 7, 9),
                12465L);
        f += Eq("buffer 2064",
                BufLive4<B2064>(ref p, 7, 9) +
                BufLive20<B2064>(ref p, 7, 9) + BufLive27<B2064>(ref p, 7, 9),
                12465L);
        f += Eq("buffer 4096",
                BufLive4<B4096>(ref p, 7, 9) +
                BufLive20<B4096>(ref p, 7, 9) + BufLive27<B4096>(ref p, 7, 9),
                12465L);
        return f;
    }

    static int Main()
    {
        for (int i = 0; i < words.Length; i++) words[i] = i + 1;

        Group("plain", Plain);
        Group("multiply and switch", Multiply);
        Group("bits and interlocked", Bits);
        Group("helpers and scratch", Helpers);
        Group("structs and stack arguments", Values);
        Group("many live values", Live);
        Group("buffers", Buffers);

        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails;
    }
}
