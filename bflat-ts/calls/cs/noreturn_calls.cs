// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Calls of methods that never return.
//
// Two things in our runtime depend on a method being known not to return.
// The compiler tells the JIT about every method marked [DoesNotReturn], so
// the code after a call of one is dead: nothing is kept alive across the
// call and the block ends there. And on a zkVM guest the JIT turns such a
// call, when it ends a block, into a jump: the caller pops its frame and
// restores its registers first, as for a tail call, and the exception then
// unwinds from the helper straight into the caller's caller.
//
// Both are wrong if the call does return, or if the caller's frame is still
// needed. The cases:
//   - guards in front of plain code, one and several, with values that are
//     used after the guard, after a normal call, in a loop, in a switch;
//   - callee-saved registers in use at the jump, up to all twelve, with and
//     without a normal call in the method; frames around 2040 bytes;
//   - helpers that take arguments on the stack, return a value, return a
//     struct through a buffer, are generic, are instance methods;
//   - callers whose frame has to stay: a local whose address is taken,
//     stackalloc, a try block around the call. The handler of that try has
//     to see what was stored before the call;
//   - a virtual method marked [DoesNotReturn] with an override that returns,
//     an interface method the same, and the opposite: a call through them
//     must come back when the target does;
//   - a helper without the attribute, and one that can be inlined.
//
// A helper that throws is called with a code, and the test reads that code
// from the exception it catches above the caller.
//
// Every group of cases runs under a caller that keeps twelve values across
// the call, one for every callee-saved register, and checks them after it.
//
// Each group prints one line, and one more for each case that failed; the
// run ends with ALL PASS or a FAIL count, and the guest returns the number
// of failures.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class E : Exception { public int Code; public E(int c) : base("e") { Code = c; } }

[StructLayout(LayoutKind.Sequential, Size = 16)] struct B16 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 2024)] struct B2024 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 2032)] struct B2032 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 2040)] struct B2040 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 2048)] struct B2048 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 2056)] struct B2056 { public long First; }
[StructLayout(LayoutKind.Sequential, Size = 4096)] struct B4096 { public long First; }

struct S40 { public long A, B, C, D, E; }

static class Throw
{
    [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
    public static void Code(int c) => throw new E(c);

    [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
    public static void Two(int c, long d) => throw new E(c + (int)d);

    [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
    public static long Value(int c) => throw new E(c);

    [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
    public static S40 Big(int c) => throw new E(c);

    [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
    public static void Many(long a1, long a2, long a3, long a4, long a5, long a6, long a7, long a8, long s1, long s2) =>
        throw new E((int)(a1 + a2 + a3 + a4 + a5 + a6 + a7 + a8 + s1 * 100 + s2 * 1000));

    [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
    public static void Of<T>(T v, int c) => throw new E(c + (v == null ? 0 : v.ToString().Length));

    [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
    public static void Struct(S40 s, int c) => throw new E(c + (int)(s.A + s.E));

    // No NoInlining: the JIT finds out by itself that it does not return.
    [DoesNotReturn]
    public static void Inlinable(int c) => throw new E(c);

    // No attribute: an ordinary call as far as the caller knows.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Plain(int c) => throw new E(c);
}

static class Gen<T>
{
    [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
    public static void Fail(T v, int c) => throw new E(c + (v == null ? 0 : 1));
}

class Thrower
{
    public int K;
    public long Seen;

    [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
    public void Fail(int c) => throw new E(c + K);

    [DoesNotReturn]
    public virtual void MayFail(int c) => throw new E(c + K);
}

// Overrides a method that does not return with one that does.
class Quiet : Thrower
{
    public override void MayFail(int c) { Seen = c; }
}

class Lenient
{
    public long Seen;
    public virtual void Check(int c) { Seen = c; }
}

// Overrides a method that returns with one that does not.
sealed class Strict : Lenient
{
    [DoesNotReturn]
    public override void Check(int c) => throw new E(c);
}

interface IFail
{
    [DoesNotReturn]
    void Fail(int c);
}

class RealFail : IFail
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Fail(int c) => throw new E(c);
}

class FakeFail : IFail
{
    public long Seen;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Fail(int c) { Seen = c; }
}

struct Acc
{
    public long V;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public long Add(long a)
    {
        if (a < 0) Throw.Code(26);
        V += a;
        return V;
    }
}

class Program
{
    static int fails;
    static long counter;

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

    // ---- guards ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Guard(long a, int c)
    {
        if (a < 0) Throw.Code(c);
        return a + 1;
    }

    // The value computed before the guard is used after it.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long GuardLive(long a, long b)
    {
        long x = a * 3 + b;
        if (b < 0) Throw.Code(2);
        return x + a;
    }

    // The same with a helper that has no attribute.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long GuardLivePlain(long a, long b)
    {
        long x = a * 3 + b;
        if (b < 0) Throw.Plain(2);
        return x + a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long GuardInlinable(long a, long b)
    {
        long x = a * 3 + b;
        if (b < 0) Throw.Inlinable(3);
        return x + a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Guards(long a, long b, long c)
    {
        if (a < 0) Throw.Code(11);
        if (b < 0) Throw.Two(12, a);
        if (c < 0) Throw.Code(13);
        return a + b * 10 + c * 100;
    }

    // A normal call first: RA and callee-saved registers are saved, and
    // restored before the jump.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long AfterCall(long a, long b)
    {
        long t = Op(a);
        long u = Op(b);
        if (t < 0) Throw.Two(3, u);
        return t + u + a + b;
    }

    // Twelve values across a call: every callee-saved register is in use.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Twelve(long a, long b)
    {
        long v0 = Op(a), v1 = Op(a + 1), v2 = Op(a + 2), v3 = Op(a + 3);
        long v4 = Op(a + 4), v5 = Op(a + 5), v6 = Op(a + 6), v7 = Op(a + 7);
        long v8 = Op(a + 8), v9 = Op(a + 9), v10 = Op(a + 10), v11 = Op(a + 11);
        long t = Op(b);
        if (t < 0) Throw.Two((int)v0, v11);
        return v0 + v1 + v2 + v3 + v4 + v5 + v6 + v7 + v8 + v9 + v10 + v11 + t;
    }

    // Callee-saved registers in use and no call but the one that does not
    // return: they are saved in a frame without a slot for RA.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Live14(ref long p, long a)
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
        if (a < 0) Throw.Two(70, s + x);
        return s + x + a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Live18(ref long p, long a)
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
        if (a < 0) Throw.Two(70, s + x);
        return s + x + a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Live22(ref long p, long a)
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
        if (a < 0) Throw.Two(70, s + x);
        return s + x + a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Live26(ref long p, long a)
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
        if (a < 0) Throw.Two(70, s + x);
        return s + x + a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long StackArgs(long a1, long a2, long a3, long a4, long a5, long a6, long a7, long a8,
                          long s1, long s2, long s3)
    {
        if (s3 < 0) Throw.Two((int)s1, s2);
        return a1 + a8 + s1 + s2 + s3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Loop(int n, long limit)
    {
        long sum = 0;
        for (int i = 1; i <= n; i++)
        {
            sum += i;
            if (sum > limit) Throw.Code(i);
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Switch(long a, long b)
    {
        switch (a)
        {
            case 0: Throw.Code(20); break;
            case 1: return b + 1;
            case 2: Throw.Two(21, b); break;
            case 3: return b * 2;
            case 4: Throw.Of("abc", 22); break;
            case 5: Throw.Plain(23); break;
        }
        return b;
    }

    // The whole method ends in the call.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Always(int c)
    {
        counter++;
        Throw.Code(c);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void Require(bool ok, int c)
    {
        if (!ok) Throw.Code(c);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Requires(long a, long b)
    {
        Require(a >= 0, 31);
        long x = a * b;
        Require(b >= 0, 32);
        Require(x < 1000, 33);
        return x;
    }

    // Many frames above the jump.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Deep(int n, long a)
    {
        if (n == 0) return Guard(a, 34);
        return Deep(n - 1, a) + 1;
    }

    // ---- large frames ----

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static long At(ref long p, int i) => Unsafe.Add(ref p, i);

    // So many values alive that most of them are kept in the frame, which
    // gets to 2040 bytes and over, where it is popped in two steps before
    // the jump.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Wide266(ref long p, long a)
    {
        long v0 = At(ref p, 0); long v1 = At(ref p, 1); long v2 = At(ref p, 2); long v3 = At(ref p, 3);
        long v4 = At(ref p, 4); long v5 = At(ref p, 5); long v6 = At(ref p, 6); long v7 = At(ref p, 7);
        long v8 = At(ref p, 8); long v9 = At(ref p, 9); long v10 = At(ref p, 10); long v11 = At(ref p, 11);
        long v12 = At(ref p, 12); long v13 = At(ref p, 13); long v14 = At(ref p, 14); long v15 = At(ref p, 15);
        long v16 = At(ref p, 16); long v17 = At(ref p, 17); long v18 = At(ref p, 18); long v19 = At(ref p, 19);
        long v20 = At(ref p, 20); long v21 = At(ref p, 21); long v22 = At(ref p, 22); long v23 = At(ref p, 23);
        long v24 = At(ref p, 24); long v25 = At(ref p, 25); long v26 = At(ref p, 26); long v27 = At(ref p, 27);
        long v28 = At(ref p, 28); long v29 = At(ref p, 29); long v30 = At(ref p, 30); long v31 = At(ref p, 31);
        long v32 = At(ref p, 32); long v33 = At(ref p, 33); long v34 = At(ref p, 34); long v35 = At(ref p, 35);
        long v36 = At(ref p, 36); long v37 = At(ref p, 37); long v38 = At(ref p, 38); long v39 = At(ref p, 39);
        long v40 = At(ref p, 40); long v41 = At(ref p, 41); long v42 = At(ref p, 42); long v43 = At(ref p, 43);
        long v44 = At(ref p, 44); long v45 = At(ref p, 45); long v46 = At(ref p, 46); long v47 = At(ref p, 47);
        long v48 = At(ref p, 48); long v49 = At(ref p, 49); long v50 = At(ref p, 50); long v51 = At(ref p, 51);
        long v52 = At(ref p, 52); long v53 = At(ref p, 53); long v54 = At(ref p, 54); long v55 = At(ref p, 55);
        long v56 = At(ref p, 56); long v57 = At(ref p, 57); long v58 = At(ref p, 58); long v59 = At(ref p, 59);
        long v60 = At(ref p, 60); long v61 = At(ref p, 61); long v62 = At(ref p, 62); long v63 = At(ref p, 63);
        long v64 = At(ref p, 64); long v65 = At(ref p, 65); long v66 = At(ref p, 66); long v67 = At(ref p, 67);
        long v68 = At(ref p, 68); long v69 = At(ref p, 69); long v70 = At(ref p, 70); long v71 = At(ref p, 71);
        long v72 = At(ref p, 72); long v73 = At(ref p, 73); long v74 = At(ref p, 74); long v75 = At(ref p, 75);
        long v76 = At(ref p, 76); long v77 = At(ref p, 77); long v78 = At(ref p, 78); long v79 = At(ref p, 79);
        long v80 = At(ref p, 80); long v81 = At(ref p, 81); long v82 = At(ref p, 82); long v83 = At(ref p, 83);
        long v84 = At(ref p, 84); long v85 = At(ref p, 85); long v86 = At(ref p, 86); long v87 = At(ref p, 87);
        long v88 = At(ref p, 88); long v89 = At(ref p, 89); long v90 = At(ref p, 90); long v91 = At(ref p, 91);
        long v92 = At(ref p, 92); long v93 = At(ref p, 93); long v94 = At(ref p, 94); long v95 = At(ref p, 95);
        long v96 = At(ref p, 96); long v97 = At(ref p, 97); long v98 = At(ref p, 98); long v99 = At(ref p, 99);
        long v100 = At(ref p, 100); long v101 = At(ref p, 101); long v102 = At(ref p, 102); long v103 = At(ref p, 103);
        long v104 = At(ref p, 104); long v105 = At(ref p, 105); long v106 = At(ref p, 106); long v107 = At(ref p, 107);
        long v108 = At(ref p, 108); long v109 = At(ref p, 109); long v110 = At(ref p, 110); long v111 = At(ref p, 111);
        long v112 = At(ref p, 112); long v113 = At(ref p, 113); long v114 = At(ref p, 114); long v115 = At(ref p, 115);
        long v116 = At(ref p, 116); long v117 = At(ref p, 117); long v118 = At(ref p, 118); long v119 = At(ref p, 119);
        long v120 = At(ref p, 120); long v121 = At(ref p, 121); long v122 = At(ref p, 122); long v123 = At(ref p, 123);
        long v124 = At(ref p, 124); long v125 = At(ref p, 125); long v126 = At(ref p, 126); long v127 = At(ref p, 127);
        long v128 = At(ref p, 128); long v129 = At(ref p, 129); long v130 = At(ref p, 130); long v131 = At(ref p, 131);
        long v132 = At(ref p, 132); long v133 = At(ref p, 133); long v134 = At(ref p, 134); long v135 = At(ref p, 135);
        long v136 = At(ref p, 136); long v137 = At(ref p, 137); long v138 = At(ref p, 138); long v139 = At(ref p, 139);
        long v140 = At(ref p, 140); long v141 = At(ref p, 141); long v142 = At(ref p, 142); long v143 = At(ref p, 143);
        long v144 = At(ref p, 144); long v145 = At(ref p, 145); long v146 = At(ref p, 146); long v147 = At(ref p, 147);
        long v148 = At(ref p, 148); long v149 = At(ref p, 149); long v150 = At(ref p, 150); long v151 = At(ref p, 151);
        long v152 = At(ref p, 152); long v153 = At(ref p, 153); long v154 = At(ref p, 154); long v155 = At(ref p, 155);
        long v156 = At(ref p, 156); long v157 = At(ref p, 157); long v158 = At(ref p, 158); long v159 = At(ref p, 159);
        long v160 = At(ref p, 160); long v161 = At(ref p, 161); long v162 = At(ref p, 162); long v163 = At(ref p, 163);
        long v164 = At(ref p, 164); long v165 = At(ref p, 165); long v166 = At(ref p, 166); long v167 = At(ref p, 167);
        long v168 = At(ref p, 168); long v169 = At(ref p, 169); long v170 = At(ref p, 170); long v171 = At(ref p, 171);
        long v172 = At(ref p, 172); long v173 = At(ref p, 173); long v174 = At(ref p, 174); long v175 = At(ref p, 175);
        long v176 = At(ref p, 176); long v177 = At(ref p, 177); long v178 = At(ref p, 178); long v179 = At(ref p, 179);
        long v180 = At(ref p, 180); long v181 = At(ref p, 181); long v182 = At(ref p, 182); long v183 = At(ref p, 183);
        long v184 = At(ref p, 184); long v185 = At(ref p, 185); long v186 = At(ref p, 186); long v187 = At(ref p, 187);
        long v188 = At(ref p, 188); long v189 = At(ref p, 189); long v190 = At(ref p, 190); long v191 = At(ref p, 191);
        long v192 = At(ref p, 192); long v193 = At(ref p, 193); long v194 = At(ref p, 194); long v195 = At(ref p, 195);
        long v196 = At(ref p, 196); long v197 = At(ref p, 197); long v198 = At(ref p, 198); long v199 = At(ref p, 199);
        long v200 = At(ref p, 200); long v201 = At(ref p, 201); long v202 = At(ref p, 202); long v203 = At(ref p, 203);
        long v204 = At(ref p, 204); long v205 = At(ref p, 205); long v206 = At(ref p, 206); long v207 = At(ref p, 207);
        long v208 = At(ref p, 208); long v209 = At(ref p, 209); long v210 = At(ref p, 210); long v211 = At(ref p, 211);
        long v212 = At(ref p, 212); long v213 = At(ref p, 213); long v214 = At(ref p, 214); long v215 = At(ref p, 215);
        long v216 = At(ref p, 216); long v217 = At(ref p, 217); long v218 = At(ref p, 218); long v219 = At(ref p, 219);
        long v220 = At(ref p, 220); long v221 = At(ref p, 221); long v222 = At(ref p, 222); long v223 = At(ref p, 223);
        long v224 = At(ref p, 224); long v225 = At(ref p, 225); long v226 = At(ref p, 226); long v227 = At(ref p, 227);
        long v228 = At(ref p, 228); long v229 = At(ref p, 229); long v230 = At(ref p, 230); long v231 = At(ref p, 231);
        long v232 = At(ref p, 232); long v233 = At(ref p, 233); long v234 = At(ref p, 234); long v235 = At(ref p, 235);
        long v236 = At(ref p, 236); long v237 = At(ref p, 237); long v238 = At(ref p, 238); long v239 = At(ref p, 239);
        long v240 = At(ref p, 240); long v241 = At(ref p, 241); long v242 = At(ref p, 242); long v243 = At(ref p, 243);
        long v244 = At(ref p, 244); long v245 = At(ref p, 245); long v246 = At(ref p, 246); long v247 = At(ref p, 247);
        long v248 = At(ref p, 248); long v249 = At(ref p, 249); long v250 = At(ref p, 250); long v251 = At(ref p, 251);
        long v252 = At(ref p, 252); long v253 = At(ref p, 253); long v254 = At(ref p, 254); long v255 = At(ref p, 255);
        long v256 = At(ref p, 256); long v257 = At(ref p, 257); long v258 = At(ref p, 258); long v259 = At(ref p, 259);
        long v260 = At(ref p, 260); long v261 = At(ref p, 261); long v262 = At(ref p, 262); long v263 = At(ref p, 263);
        long v264 = At(ref p, 264); long v265 = At(ref p, 265);
        long s = v0 - v1 + v2 - v3 + v4 - v5 + v6 - v7 + v8 - v9 + v10 - v11 + v12 - v13
               + v14 - v15 + v16 - v17 + v18 - v19 + v20 - v21 + v22 - v23 + v24 - v25 + v26 - v27
               + v28 - v29 + v30 - v31 + v32 - v33 + v34 - v35 + v36 - v37 + v38 - v39 + v40 - v41
               + v42 - v43 + v44 - v45 + v46 - v47 + v48 - v49 + v50 - v51 + v52 - v53 + v54 - v55
               + v56 - v57 + v58 - v59 + v60 - v61 + v62 - v63 + v64 - v65 + v66 - v67 + v68 - v69
               + v70 - v71 + v72 - v73 + v74 - v75 + v76 - v77 + v78 - v79 + v80 - v81 + v82 - v83
               + v84 - v85 + v86 - v87 + v88 - v89 + v90 - v91 + v92 - v93 + v94 - v95 + v96 - v97
               + v98 - v99 + v100 - v101 + v102 - v103 + v104 - v105 + v106 - v107 + v108 - v109 + v110 - v111
               + v112 - v113 + v114 - v115 + v116 - v117 + v118 - v119 + v120 - v121 + v122 - v123 + v124 - v125
               + v126 - v127 + v128 - v129 + v130 - v131 + v132 - v133 + v134 - v135 + v136 - v137 + v138 - v139
               + v140 - v141 + v142 - v143 + v144 - v145 + v146 - v147 + v148 - v149 + v150 - v151 + v152 - v153
               + v154 - v155 + v156 - v157 + v158 - v159 + v160 - v161 + v162 - v163 + v164 - v165 + v166 - v167
               + v168 - v169 + v170 - v171 + v172 - v173 + v174 - v175 + v176 - v177 + v178 - v179 + v180 - v181
               + v182 - v183 + v184 - v185 + v186 - v187 + v188 - v189 + v190 - v191 + v192 - v193 + v194 - v195
               + v196 - v197 + v198 - v199 + v200 - v201 + v202 - v203 + v204 - v205 + v206 - v207 + v208 - v209
               + v210 - v211 + v212 - v213 + v214 - v215 + v216 - v217 + v218 - v219 + v220 - v221 + v222 - v223
               + v224 - v225 + v226 - v227 + v228 - v229 + v230 - v231 + v232 - v233 + v234 - v235 + v236 - v237
               + v238 - v239 + v240 - v241 + v242 - v243 + v244 - v245 + v246 - v247 + v248 - v249 + v250 - v251
               + v252 - v253 + v254 - v255 + v256 - v257 + v258 - v259 + v260 - v261 + v262 - v263 + v264 - v265;
        if (a < 0) Throw.Two(71, s);
        return s + a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Wide267(ref long p, long a)
    {
        long v0 = At(ref p, 0); long v1 = At(ref p, 1); long v2 = At(ref p, 2); long v3 = At(ref p, 3);
        long v4 = At(ref p, 4); long v5 = At(ref p, 5); long v6 = At(ref p, 6); long v7 = At(ref p, 7);
        long v8 = At(ref p, 8); long v9 = At(ref p, 9); long v10 = At(ref p, 10); long v11 = At(ref p, 11);
        long v12 = At(ref p, 12); long v13 = At(ref p, 13); long v14 = At(ref p, 14); long v15 = At(ref p, 15);
        long v16 = At(ref p, 16); long v17 = At(ref p, 17); long v18 = At(ref p, 18); long v19 = At(ref p, 19);
        long v20 = At(ref p, 20); long v21 = At(ref p, 21); long v22 = At(ref p, 22); long v23 = At(ref p, 23);
        long v24 = At(ref p, 24); long v25 = At(ref p, 25); long v26 = At(ref p, 26); long v27 = At(ref p, 27);
        long v28 = At(ref p, 28); long v29 = At(ref p, 29); long v30 = At(ref p, 30); long v31 = At(ref p, 31);
        long v32 = At(ref p, 32); long v33 = At(ref p, 33); long v34 = At(ref p, 34); long v35 = At(ref p, 35);
        long v36 = At(ref p, 36); long v37 = At(ref p, 37); long v38 = At(ref p, 38); long v39 = At(ref p, 39);
        long v40 = At(ref p, 40); long v41 = At(ref p, 41); long v42 = At(ref p, 42); long v43 = At(ref p, 43);
        long v44 = At(ref p, 44); long v45 = At(ref p, 45); long v46 = At(ref p, 46); long v47 = At(ref p, 47);
        long v48 = At(ref p, 48); long v49 = At(ref p, 49); long v50 = At(ref p, 50); long v51 = At(ref p, 51);
        long v52 = At(ref p, 52); long v53 = At(ref p, 53); long v54 = At(ref p, 54); long v55 = At(ref p, 55);
        long v56 = At(ref p, 56); long v57 = At(ref p, 57); long v58 = At(ref p, 58); long v59 = At(ref p, 59);
        long v60 = At(ref p, 60); long v61 = At(ref p, 61); long v62 = At(ref p, 62); long v63 = At(ref p, 63);
        long v64 = At(ref p, 64); long v65 = At(ref p, 65); long v66 = At(ref p, 66); long v67 = At(ref p, 67);
        long v68 = At(ref p, 68); long v69 = At(ref p, 69); long v70 = At(ref p, 70); long v71 = At(ref p, 71);
        long v72 = At(ref p, 72); long v73 = At(ref p, 73); long v74 = At(ref p, 74); long v75 = At(ref p, 75);
        long v76 = At(ref p, 76); long v77 = At(ref p, 77); long v78 = At(ref p, 78); long v79 = At(ref p, 79);
        long v80 = At(ref p, 80); long v81 = At(ref p, 81); long v82 = At(ref p, 82); long v83 = At(ref p, 83);
        long v84 = At(ref p, 84); long v85 = At(ref p, 85); long v86 = At(ref p, 86); long v87 = At(ref p, 87);
        long v88 = At(ref p, 88); long v89 = At(ref p, 89); long v90 = At(ref p, 90); long v91 = At(ref p, 91);
        long v92 = At(ref p, 92); long v93 = At(ref p, 93); long v94 = At(ref p, 94); long v95 = At(ref p, 95);
        long v96 = At(ref p, 96); long v97 = At(ref p, 97); long v98 = At(ref p, 98); long v99 = At(ref p, 99);
        long v100 = At(ref p, 100); long v101 = At(ref p, 101); long v102 = At(ref p, 102); long v103 = At(ref p, 103);
        long v104 = At(ref p, 104); long v105 = At(ref p, 105); long v106 = At(ref p, 106); long v107 = At(ref p, 107);
        long v108 = At(ref p, 108); long v109 = At(ref p, 109); long v110 = At(ref p, 110); long v111 = At(ref p, 111);
        long v112 = At(ref p, 112); long v113 = At(ref p, 113); long v114 = At(ref p, 114); long v115 = At(ref p, 115);
        long v116 = At(ref p, 116); long v117 = At(ref p, 117); long v118 = At(ref p, 118); long v119 = At(ref p, 119);
        long v120 = At(ref p, 120); long v121 = At(ref p, 121); long v122 = At(ref p, 122); long v123 = At(ref p, 123);
        long v124 = At(ref p, 124); long v125 = At(ref p, 125); long v126 = At(ref p, 126); long v127 = At(ref p, 127);
        long v128 = At(ref p, 128); long v129 = At(ref p, 129); long v130 = At(ref p, 130); long v131 = At(ref p, 131);
        long v132 = At(ref p, 132); long v133 = At(ref p, 133); long v134 = At(ref p, 134); long v135 = At(ref p, 135);
        long v136 = At(ref p, 136); long v137 = At(ref p, 137); long v138 = At(ref p, 138); long v139 = At(ref p, 139);
        long v140 = At(ref p, 140); long v141 = At(ref p, 141); long v142 = At(ref p, 142); long v143 = At(ref p, 143);
        long v144 = At(ref p, 144); long v145 = At(ref p, 145); long v146 = At(ref p, 146); long v147 = At(ref p, 147);
        long v148 = At(ref p, 148); long v149 = At(ref p, 149); long v150 = At(ref p, 150); long v151 = At(ref p, 151);
        long v152 = At(ref p, 152); long v153 = At(ref p, 153); long v154 = At(ref p, 154); long v155 = At(ref p, 155);
        long v156 = At(ref p, 156); long v157 = At(ref p, 157); long v158 = At(ref p, 158); long v159 = At(ref p, 159);
        long v160 = At(ref p, 160); long v161 = At(ref p, 161); long v162 = At(ref p, 162); long v163 = At(ref p, 163);
        long v164 = At(ref p, 164); long v165 = At(ref p, 165); long v166 = At(ref p, 166); long v167 = At(ref p, 167);
        long v168 = At(ref p, 168); long v169 = At(ref p, 169); long v170 = At(ref p, 170); long v171 = At(ref p, 171);
        long v172 = At(ref p, 172); long v173 = At(ref p, 173); long v174 = At(ref p, 174); long v175 = At(ref p, 175);
        long v176 = At(ref p, 176); long v177 = At(ref p, 177); long v178 = At(ref p, 178); long v179 = At(ref p, 179);
        long v180 = At(ref p, 180); long v181 = At(ref p, 181); long v182 = At(ref p, 182); long v183 = At(ref p, 183);
        long v184 = At(ref p, 184); long v185 = At(ref p, 185); long v186 = At(ref p, 186); long v187 = At(ref p, 187);
        long v188 = At(ref p, 188); long v189 = At(ref p, 189); long v190 = At(ref p, 190); long v191 = At(ref p, 191);
        long v192 = At(ref p, 192); long v193 = At(ref p, 193); long v194 = At(ref p, 194); long v195 = At(ref p, 195);
        long v196 = At(ref p, 196); long v197 = At(ref p, 197); long v198 = At(ref p, 198); long v199 = At(ref p, 199);
        long v200 = At(ref p, 200); long v201 = At(ref p, 201); long v202 = At(ref p, 202); long v203 = At(ref p, 203);
        long v204 = At(ref p, 204); long v205 = At(ref p, 205); long v206 = At(ref p, 206); long v207 = At(ref p, 207);
        long v208 = At(ref p, 208); long v209 = At(ref p, 209); long v210 = At(ref p, 210); long v211 = At(ref p, 211);
        long v212 = At(ref p, 212); long v213 = At(ref p, 213); long v214 = At(ref p, 214); long v215 = At(ref p, 215);
        long v216 = At(ref p, 216); long v217 = At(ref p, 217); long v218 = At(ref p, 218); long v219 = At(ref p, 219);
        long v220 = At(ref p, 220); long v221 = At(ref p, 221); long v222 = At(ref p, 222); long v223 = At(ref p, 223);
        long v224 = At(ref p, 224); long v225 = At(ref p, 225); long v226 = At(ref p, 226); long v227 = At(ref p, 227);
        long v228 = At(ref p, 228); long v229 = At(ref p, 229); long v230 = At(ref p, 230); long v231 = At(ref p, 231);
        long v232 = At(ref p, 232); long v233 = At(ref p, 233); long v234 = At(ref p, 234); long v235 = At(ref p, 235);
        long v236 = At(ref p, 236); long v237 = At(ref p, 237); long v238 = At(ref p, 238); long v239 = At(ref p, 239);
        long v240 = At(ref p, 240); long v241 = At(ref p, 241); long v242 = At(ref p, 242); long v243 = At(ref p, 243);
        long v244 = At(ref p, 244); long v245 = At(ref p, 245); long v246 = At(ref p, 246); long v247 = At(ref p, 247);
        long v248 = At(ref p, 248); long v249 = At(ref p, 249); long v250 = At(ref p, 250); long v251 = At(ref p, 251);
        long v252 = At(ref p, 252); long v253 = At(ref p, 253); long v254 = At(ref p, 254); long v255 = At(ref p, 255);
        long v256 = At(ref p, 256); long v257 = At(ref p, 257); long v258 = At(ref p, 258); long v259 = At(ref p, 259);
        long v260 = At(ref p, 260); long v261 = At(ref p, 261); long v262 = At(ref p, 262); long v263 = At(ref p, 263);
        long v264 = At(ref p, 264); long v265 = At(ref p, 265); long v266 = At(ref p, 266);
        long s = v0 - v1 + v2 - v3 + v4 - v5 + v6 - v7 + v8 - v9 + v10 - v11 + v12 - v13
               + v14 - v15 + v16 - v17 + v18 - v19 + v20 - v21 + v22 - v23 + v24 - v25 + v26 - v27
               + v28 - v29 + v30 - v31 + v32 - v33 + v34 - v35 + v36 - v37 + v38 - v39 + v40 - v41
               + v42 - v43 + v44 - v45 + v46 - v47 + v48 - v49 + v50 - v51 + v52 - v53 + v54 - v55
               + v56 - v57 + v58 - v59 + v60 - v61 + v62 - v63 + v64 - v65 + v66 - v67 + v68 - v69
               + v70 - v71 + v72 - v73 + v74 - v75 + v76 - v77 + v78 - v79 + v80 - v81 + v82 - v83
               + v84 - v85 + v86 - v87 + v88 - v89 + v90 - v91 + v92 - v93 + v94 - v95 + v96 - v97
               + v98 - v99 + v100 - v101 + v102 - v103 + v104 - v105 + v106 - v107 + v108 - v109 + v110 - v111
               + v112 - v113 + v114 - v115 + v116 - v117 + v118 - v119 + v120 - v121 + v122 - v123 + v124 - v125
               + v126 - v127 + v128 - v129 + v130 - v131 + v132 - v133 + v134 - v135 + v136 - v137 + v138 - v139
               + v140 - v141 + v142 - v143 + v144 - v145 + v146 - v147 + v148 - v149 + v150 - v151 + v152 - v153
               + v154 - v155 + v156 - v157 + v158 - v159 + v160 - v161 + v162 - v163 + v164 - v165 + v166 - v167
               + v168 - v169 + v170 - v171 + v172 - v173 + v174 - v175 + v176 - v177 + v178 - v179 + v180 - v181
               + v182 - v183 + v184 - v185 + v186 - v187 + v188 - v189 + v190 - v191 + v192 - v193 + v194 - v195
               + v196 - v197 + v198 - v199 + v200 - v201 + v202 - v203 + v204 - v205 + v206 - v207 + v208 - v209
               + v210 - v211 + v212 - v213 + v214 - v215 + v216 - v217 + v218 - v219 + v220 - v221 + v222 - v223
               + v224 - v225 + v226 - v227 + v228 - v229 + v230 - v231 + v232 - v233 + v234 - v235 + v236 - v237
               + v238 - v239 + v240 - v241 + v242 - v243 + v244 - v245 + v246 - v247 + v248 - v249 + v250 - v251
               + v252 - v253 + v254 - v255 + v256 - v257 + v258 - v259 + v260 - v261 + v262 - v263 + v264 - v265
               + v266;
        if (a < 0) Throw.Two(71, s);
        return s + a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Wide268(ref long p, long a)
    {
        long v0 = At(ref p, 0); long v1 = At(ref p, 1); long v2 = At(ref p, 2); long v3 = At(ref p, 3);
        long v4 = At(ref p, 4); long v5 = At(ref p, 5); long v6 = At(ref p, 6); long v7 = At(ref p, 7);
        long v8 = At(ref p, 8); long v9 = At(ref p, 9); long v10 = At(ref p, 10); long v11 = At(ref p, 11);
        long v12 = At(ref p, 12); long v13 = At(ref p, 13); long v14 = At(ref p, 14); long v15 = At(ref p, 15);
        long v16 = At(ref p, 16); long v17 = At(ref p, 17); long v18 = At(ref p, 18); long v19 = At(ref p, 19);
        long v20 = At(ref p, 20); long v21 = At(ref p, 21); long v22 = At(ref p, 22); long v23 = At(ref p, 23);
        long v24 = At(ref p, 24); long v25 = At(ref p, 25); long v26 = At(ref p, 26); long v27 = At(ref p, 27);
        long v28 = At(ref p, 28); long v29 = At(ref p, 29); long v30 = At(ref p, 30); long v31 = At(ref p, 31);
        long v32 = At(ref p, 32); long v33 = At(ref p, 33); long v34 = At(ref p, 34); long v35 = At(ref p, 35);
        long v36 = At(ref p, 36); long v37 = At(ref p, 37); long v38 = At(ref p, 38); long v39 = At(ref p, 39);
        long v40 = At(ref p, 40); long v41 = At(ref p, 41); long v42 = At(ref p, 42); long v43 = At(ref p, 43);
        long v44 = At(ref p, 44); long v45 = At(ref p, 45); long v46 = At(ref p, 46); long v47 = At(ref p, 47);
        long v48 = At(ref p, 48); long v49 = At(ref p, 49); long v50 = At(ref p, 50); long v51 = At(ref p, 51);
        long v52 = At(ref p, 52); long v53 = At(ref p, 53); long v54 = At(ref p, 54); long v55 = At(ref p, 55);
        long v56 = At(ref p, 56); long v57 = At(ref p, 57); long v58 = At(ref p, 58); long v59 = At(ref p, 59);
        long v60 = At(ref p, 60); long v61 = At(ref p, 61); long v62 = At(ref p, 62); long v63 = At(ref p, 63);
        long v64 = At(ref p, 64); long v65 = At(ref p, 65); long v66 = At(ref p, 66); long v67 = At(ref p, 67);
        long v68 = At(ref p, 68); long v69 = At(ref p, 69); long v70 = At(ref p, 70); long v71 = At(ref p, 71);
        long v72 = At(ref p, 72); long v73 = At(ref p, 73); long v74 = At(ref p, 74); long v75 = At(ref p, 75);
        long v76 = At(ref p, 76); long v77 = At(ref p, 77); long v78 = At(ref p, 78); long v79 = At(ref p, 79);
        long v80 = At(ref p, 80); long v81 = At(ref p, 81); long v82 = At(ref p, 82); long v83 = At(ref p, 83);
        long v84 = At(ref p, 84); long v85 = At(ref p, 85); long v86 = At(ref p, 86); long v87 = At(ref p, 87);
        long v88 = At(ref p, 88); long v89 = At(ref p, 89); long v90 = At(ref p, 90); long v91 = At(ref p, 91);
        long v92 = At(ref p, 92); long v93 = At(ref p, 93); long v94 = At(ref p, 94); long v95 = At(ref p, 95);
        long v96 = At(ref p, 96); long v97 = At(ref p, 97); long v98 = At(ref p, 98); long v99 = At(ref p, 99);
        long v100 = At(ref p, 100); long v101 = At(ref p, 101); long v102 = At(ref p, 102); long v103 = At(ref p, 103);
        long v104 = At(ref p, 104); long v105 = At(ref p, 105); long v106 = At(ref p, 106); long v107 = At(ref p, 107);
        long v108 = At(ref p, 108); long v109 = At(ref p, 109); long v110 = At(ref p, 110); long v111 = At(ref p, 111);
        long v112 = At(ref p, 112); long v113 = At(ref p, 113); long v114 = At(ref p, 114); long v115 = At(ref p, 115);
        long v116 = At(ref p, 116); long v117 = At(ref p, 117); long v118 = At(ref p, 118); long v119 = At(ref p, 119);
        long v120 = At(ref p, 120); long v121 = At(ref p, 121); long v122 = At(ref p, 122); long v123 = At(ref p, 123);
        long v124 = At(ref p, 124); long v125 = At(ref p, 125); long v126 = At(ref p, 126); long v127 = At(ref p, 127);
        long v128 = At(ref p, 128); long v129 = At(ref p, 129); long v130 = At(ref p, 130); long v131 = At(ref p, 131);
        long v132 = At(ref p, 132); long v133 = At(ref p, 133); long v134 = At(ref p, 134); long v135 = At(ref p, 135);
        long v136 = At(ref p, 136); long v137 = At(ref p, 137); long v138 = At(ref p, 138); long v139 = At(ref p, 139);
        long v140 = At(ref p, 140); long v141 = At(ref p, 141); long v142 = At(ref p, 142); long v143 = At(ref p, 143);
        long v144 = At(ref p, 144); long v145 = At(ref p, 145); long v146 = At(ref p, 146); long v147 = At(ref p, 147);
        long v148 = At(ref p, 148); long v149 = At(ref p, 149); long v150 = At(ref p, 150); long v151 = At(ref p, 151);
        long v152 = At(ref p, 152); long v153 = At(ref p, 153); long v154 = At(ref p, 154); long v155 = At(ref p, 155);
        long v156 = At(ref p, 156); long v157 = At(ref p, 157); long v158 = At(ref p, 158); long v159 = At(ref p, 159);
        long v160 = At(ref p, 160); long v161 = At(ref p, 161); long v162 = At(ref p, 162); long v163 = At(ref p, 163);
        long v164 = At(ref p, 164); long v165 = At(ref p, 165); long v166 = At(ref p, 166); long v167 = At(ref p, 167);
        long v168 = At(ref p, 168); long v169 = At(ref p, 169); long v170 = At(ref p, 170); long v171 = At(ref p, 171);
        long v172 = At(ref p, 172); long v173 = At(ref p, 173); long v174 = At(ref p, 174); long v175 = At(ref p, 175);
        long v176 = At(ref p, 176); long v177 = At(ref p, 177); long v178 = At(ref p, 178); long v179 = At(ref p, 179);
        long v180 = At(ref p, 180); long v181 = At(ref p, 181); long v182 = At(ref p, 182); long v183 = At(ref p, 183);
        long v184 = At(ref p, 184); long v185 = At(ref p, 185); long v186 = At(ref p, 186); long v187 = At(ref p, 187);
        long v188 = At(ref p, 188); long v189 = At(ref p, 189); long v190 = At(ref p, 190); long v191 = At(ref p, 191);
        long v192 = At(ref p, 192); long v193 = At(ref p, 193); long v194 = At(ref p, 194); long v195 = At(ref p, 195);
        long v196 = At(ref p, 196); long v197 = At(ref p, 197); long v198 = At(ref p, 198); long v199 = At(ref p, 199);
        long v200 = At(ref p, 200); long v201 = At(ref p, 201); long v202 = At(ref p, 202); long v203 = At(ref p, 203);
        long v204 = At(ref p, 204); long v205 = At(ref p, 205); long v206 = At(ref p, 206); long v207 = At(ref p, 207);
        long v208 = At(ref p, 208); long v209 = At(ref p, 209); long v210 = At(ref p, 210); long v211 = At(ref p, 211);
        long v212 = At(ref p, 212); long v213 = At(ref p, 213); long v214 = At(ref p, 214); long v215 = At(ref p, 215);
        long v216 = At(ref p, 216); long v217 = At(ref p, 217); long v218 = At(ref p, 218); long v219 = At(ref p, 219);
        long v220 = At(ref p, 220); long v221 = At(ref p, 221); long v222 = At(ref p, 222); long v223 = At(ref p, 223);
        long v224 = At(ref p, 224); long v225 = At(ref p, 225); long v226 = At(ref p, 226); long v227 = At(ref p, 227);
        long v228 = At(ref p, 228); long v229 = At(ref p, 229); long v230 = At(ref p, 230); long v231 = At(ref p, 231);
        long v232 = At(ref p, 232); long v233 = At(ref p, 233); long v234 = At(ref p, 234); long v235 = At(ref p, 235);
        long v236 = At(ref p, 236); long v237 = At(ref p, 237); long v238 = At(ref p, 238); long v239 = At(ref p, 239);
        long v240 = At(ref p, 240); long v241 = At(ref p, 241); long v242 = At(ref p, 242); long v243 = At(ref p, 243);
        long v244 = At(ref p, 244); long v245 = At(ref p, 245); long v246 = At(ref p, 246); long v247 = At(ref p, 247);
        long v248 = At(ref p, 248); long v249 = At(ref p, 249); long v250 = At(ref p, 250); long v251 = At(ref p, 251);
        long v252 = At(ref p, 252); long v253 = At(ref p, 253); long v254 = At(ref p, 254); long v255 = At(ref p, 255);
        long v256 = At(ref p, 256); long v257 = At(ref p, 257); long v258 = At(ref p, 258); long v259 = At(ref p, 259);
        long v260 = At(ref p, 260); long v261 = At(ref p, 261); long v262 = At(ref p, 262); long v263 = At(ref p, 263);
        long v264 = At(ref p, 264); long v265 = At(ref p, 265); long v266 = At(ref p, 266); long v267 = At(ref p, 267);
        long s = v0 - v1 + v2 - v3 + v4 - v5 + v6 - v7 + v8 - v9 + v10 - v11 + v12 - v13
               + v14 - v15 + v16 - v17 + v18 - v19 + v20 - v21 + v22 - v23 + v24 - v25 + v26 - v27
               + v28 - v29 + v30 - v31 + v32 - v33 + v34 - v35 + v36 - v37 + v38 - v39 + v40 - v41
               + v42 - v43 + v44 - v45 + v46 - v47 + v48 - v49 + v50 - v51 + v52 - v53 + v54 - v55
               + v56 - v57 + v58 - v59 + v60 - v61 + v62 - v63 + v64 - v65 + v66 - v67 + v68 - v69
               + v70 - v71 + v72 - v73 + v74 - v75 + v76 - v77 + v78 - v79 + v80 - v81 + v82 - v83
               + v84 - v85 + v86 - v87 + v88 - v89 + v90 - v91 + v92 - v93 + v94 - v95 + v96 - v97
               + v98 - v99 + v100 - v101 + v102 - v103 + v104 - v105 + v106 - v107 + v108 - v109 + v110 - v111
               + v112 - v113 + v114 - v115 + v116 - v117 + v118 - v119 + v120 - v121 + v122 - v123 + v124 - v125
               + v126 - v127 + v128 - v129 + v130 - v131 + v132 - v133 + v134 - v135 + v136 - v137 + v138 - v139
               + v140 - v141 + v142 - v143 + v144 - v145 + v146 - v147 + v148 - v149 + v150 - v151 + v152 - v153
               + v154 - v155 + v156 - v157 + v158 - v159 + v160 - v161 + v162 - v163 + v164 - v165 + v166 - v167
               + v168 - v169 + v170 - v171 + v172 - v173 + v174 - v175 + v176 - v177 + v178 - v179 + v180 - v181
               + v182 - v183 + v184 - v185 + v186 - v187 + v188 - v189 + v190 - v191 + v192 - v193 + v194 - v195
               + v196 - v197 + v198 - v199 + v200 - v201 + v202 - v203 + v204 - v205 + v206 - v207 + v208 - v209
               + v210 - v211 + v212 - v213 + v214 - v215 + v216 - v217 + v218 - v219 + v220 - v221 + v222 - v223
               + v224 - v225 + v226 - v227 + v228 - v229 + v230 - v231 + v232 - v233 + v234 - v235 + v236 - v237
               + v238 - v239 + v240 - v241 + v242 - v243 + v244 - v245 + v246 - v247 + v248 - v249 + v250 - v251
               + v252 - v253 + v254 - v255 + v256 - v257 + v258 - v259 + v260 - v261 + v262 - v263 + v264 - v265
               + v266 - v267;
        if (a < 0) Throw.Two(71, s);
        return s + a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Wide300(ref long p, long a)
    {
        long v0 = At(ref p, 0); long v1 = At(ref p, 1); long v2 = At(ref p, 2); long v3 = At(ref p, 3);
        long v4 = At(ref p, 4); long v5 = At(ref p, 5); long v6 = At(ref p, 6); long v7 = At(ref p, 7);
        long v8 = At(ref p, 8); long v9 = At(ref p, 9); long v10 = At(ref p, 10); long v11 = At(ref p, 11);
        long v12 = At(ref p, 12); long v13 = At(ref p, 13); long v14 = At(ref p, 14); long v15 = At(ref p, 15);
        long v16 = At(ref p, 16); long v17 = At(ref p, 17); long v18 = At(ref p, 18); long v19 = At(ref p, 19);
        long v20 = At(ref p, 20); long v21 = At(ref p, 21); long v22 = At(ref p, 22); long v23 = At(ref p, 23);
        long v24 = At(ref p, 24); long v25 = At(ref p, 25); long v26 = At(ref p, 26); long v27 = At(ref p, 27);
        long v28 = At(ref p, 28); long v29 = At(ref p, 29); long v30 = At(ref p, 30); long v31 = At(ref p, 31);
        long v32 = At(ref p, 32); long v33 = At(ref p, 33); long v34 = At(ref p, 34); long v35 = At(ref p, 35);
        long v36 = At(ref p, 36); long v37 = At(ref p, 37); long v38 = At(ref p, 38); long v39 = At(ref p, 39);
        long v40 = At(ref p, 40); long v41 = At(ref p, 41); long v42 = At(ref p, 42); long v43 = At(ref p, 43);
        long v44 = At(ref p, 44); long v45 = At(ref p, 45); long v46 = At(ref p, 46); long v47 = At(ref p, 47);
        long v48 = At(ref p, 48); long v49 = At(ref p, 49); long v50 = At(ref p, 50); long v51 = At(ref p, 51);
        long v52 = At(ref p, 52); long v53 = At(ref p, 53); long v54 = At(ref p, 54); long v55 = At(ref p, 55);
        long v56 = At(ref p, 56); long v57 = At(ref p, 57); long v58 = At(ref p, 58); long v59 = At(ref p, 59);
        long v60 = At(ref p, 60); long v61 = At(ref p, 61); long v62 = At(ref p, 62); long v63 = At(ref p, 63);
        long v64 = At(ref p, 64); long v65 = At(ref p, 65); long v66 = At(ref p, 66); long v67 = At(ref p, 67);
        long v68 = At(ref p, 68); long v69 = At(ref p, 69); long v70 = At(ref p, 70); long v71 = At(ref p, 71);
        long v72 = At(ref p, 72); long v73 = At(ref p, 73); long v74 = At(ref p, 74); long v75 = At(ref p, 75);
        long v76 = At(ref p, 76); long v77 = At(ref p, 77); long v78 = At(ref p, 78); long v79 = At(ref p, 79);
        long v80 = At(ref p, 80); long v81 = At(ref p, 81); long v82 = At(ref p, 82); long v83 = At(ref p, 83);
        long v84 = At(ref p, 84); long v85 = At(ref p, 85); long v86 = At(ref p, 86); long v87 = At(ref p, 87);
        long v88 = At(ref p, 88); long v89 = At(ref p, 89); long v90 = At(ref p, 90); long v91 = At(ref p, 91);
        long v92 = At(ref p, 92); long v93 = At(ref p, 93); long v94 = At(ref p, 94); long v95 = At(ref p, 95);
        long v96 = At(ref p, 96); long v97 = At(ref p, 97); long v98 = At(ref p, 98); long v99 = At(ref p, 99);
        long v100 = At(ref p, 100); long v101 = At(ref p, 101); long v102 = At(ref p, 102); long v103 = At(ref p, 103);
        long v104 = At(ref p, 104); long v105 = At(ref p, 105); long v106 = At(ref p, 106); long v107 = At(ref p, 107);
        long v108 = At(ref p, 108); long v109 = At(ref p, 109); long v110 = At(ref p, 110); long v111 = At(ref p, 111);
        long v112 = At(ref p, 112); long v113 = At(ref p, 113); long v114 = At(ref p, 114); long v115 = At(ref p, 115);
        long v116 = At(ref p, 116); long v117 = At(ref p, 117); long v118 = At(ref p, 118); long v119 = At(ref p, 119);
        long v120 = At(ref p, 120); long v121 = At(ref p, 121); long v122 = At(ref p, 122); long v123 = At(ref p, 123);
        long v124 = At(ref p, 124); long v125 = At(ref p, 125); long v126 = At(ref p, 126); long v127 = At(ref p, 127);
        long v128 = At(ref p, 128); long v129 = At(ref p, 129); long v130 = At(ref p, 130); long v131 = At(ref p, 131);
        long v132 = At(ref p, 132); long v133 = At(ref p, 133); long v134 = At(ref p, 134); long v135 = At(ref p, 135);
        long v136 = At(ref p, 136); long v137 = At(ref p, 137); long v138 = At(ref p, 138); long v139 = At(ref p, 139);
        long v140 = At(ref p, 140); long v141 = At(ref p, 141); long v142 = At(ref p, 142); long v143 = At(ref p, 143);
        long v144 = At(ref p, 144); long v145 = At(ref p, 145); long v146 = At(ref p, 146); long v147 = At(ref p, 147);
        long v148 = At(ref p, 148); long v149 = At(ref p, 149); long v150 = At(ref p, 150); long v151 = At(ref p, 151);
        long v152 = At(ref p, 152); long v153 = At(ref p, 153); long v154 = At(ref p, 154); long v155 = At(ref p, 155);
        long v156 = At(ref p, 156); long v157 = At(ref p, 157); long v158 = At(ref p, 158); long v159 = At(ref p, 159);
        long v160 = At(ref p, 160); long v161 = At(ref p, 161); long v162 = At(ref p, 162); long v163 = At(ref p, 163);
        long v164 = At(ref p, 164); long v165 = At(ref p, 165); long v166 = At(ref p, 166); long v167 = At(ref p, 167);
        long v168 = At(ref p, 168); long v169 = At(ref p, 169); long v170 = At(ref p, 170); long v171 = At(ref p, 171);
        long v172 = At(ref p, 172); long v173 = At(ref p, 173); long v174 = At(ref p, 174); long v175 = At(ref p, 175);
        long v176 = At(ref p, 176); long v177 = At(ref p, 177); long v178 = At(ref p, 178); long v179 = At(ref p, 179);
        long v180 = At(ref p, 180); long v181 = At(ref p, 181); long v182 = At(ref p, 182); long v183 = At(ref p, 183);
        long v184 = At(ref p, 184); long v185 = At(ref p, 185); long v186 = At(ref p, 186); long v187 = At(ref p, 187);
        long v188 = At(ref p, 188); long v189 = At(ref p, 189); long v190 = At(ref p, 190); long v191 = At(ref p, 191);
        long v192 = At(ref p, 192); long v193 = At(ref p, 193); long v194 = At(ref p, 194); long v195 = At(ref p, 195);
        long v196 = At(ref p, 196); long v197 = At(ref p, 197); long v198 = At(ref p, 198); long v199 = At(ref p, 199);
        long v200 = At(ref p, 200); long v201 = At(ref p, 201); long v202 = At(ref p, 202); long v203 = At(ref p, 203);
        long v204 = At(ref p, 204); long v205 = At(ref p, 205); long v206 = At(ref p, 206); long v207 = At(ref p, 207);
        long v208 = At(ref p, 208); long v209 = At(ref p, 209); long v210 = At(ref p, 210); long v211 = At(ref p, 211);
        long v212 = At(ref p, 212); long v213 = At(ref p, 213); long v214 = At(ref p, 214); long v215 = At(ref p, 215);
        long v216 = At(ref p, 216); long v217 = At(ref p, 217); long v218 = At(ref p, 218); long v219 = At(ref p, 219);
        long v220 = At(ref p, 220); long v221 = At(ref p, 221); long v222 = At(ref p, 222); long v223 = At(ref p, 223);
        long v224 = At(ref p, 224); long v225 = At(ref p, 225); long v226 = At(ref p, 226); long v227 = At(ref p, 227);
        long v228 = At(ref p, 228); long v229 = At(ref p, 229); long v230 = At(ref p, 230); long v231 = At(ref p, 231);
        long v232 = At(ref p, 232); long v233 = At(ref p, 233); long v234 = At(ref p, 234); long v235 = At(ref p, 235);
        long v236 = At(ref p, 236); long v237 = At(ref p, 237); long v238 = At(ref p, 238); long v239 = At(ref p, 239);
        long v240 = At(ref p, 240); long v241 = At(ref p, 241); long v242 = At(ref p, 242); long v243 = At(ref p, 243);
        long v244 = At(ref p, 244); long v245 = At(ref p, 245); long v246 = At(ref p, 246); long v247 = At(ref p, 247);
        long v248 = At(ref p, 248); long v249 = At(ref p, 249); long v250 = At(ref p, 250); long v251 = At(ref p, 251);
        long v252 = At(ref p, 252); long v253 = At(ref p, 253); long v254 = At(ref p, 254); long v255 = At(ref p, 255);
        long v256 = At(ref p, 256); long v257 = At(ref p, 257); long v258 = At(ref p, 258); long v259 = At(ref p, 259);
        long v260 = At(ref p, 260); long v261 = At(ref p, 261); long v262 = At(ref p, 262); long v263 = At(ref p, 263);
        long v264 = At(ref p, 264); long v265 = At(ref p, 265); long v266 = At(ref p, 266); long v267 = At(ref p, 267);
        long v268 = At(ref p, 268); long v269 = At(ref p, 269); long v270 = At(ref p, 270); long v271 = At(ref p, 271);
        long v272 = At(ref p, 272); long v273 = At(ref p, 273); long v274 = At(ref p, 274); long v275 = At(ref p, 275);
        long v276 = At(ref p, 276); long v277 = At(ref p, 277); long v278 = At(ref p, 278); long v279 = At(ref p, 279);
        long v280 = At(ref p, 280); long v281 = At(ref p, 281); long v282 = At(ref p, 282); long v283 = At(ref p, 283);
        long v284 = At(ref p, 284); long v285 = At(ref p, 285); long v286 = At(ref p, 286); long v287 = At(ref p, 287);
        long v288 = At(ref p, 288); long v289 = At(ref p, 289); long v290 = At(ref p, 290); long v291 = At(ref p, 291);
        long v292 = At(ref p, 292); long v293 = At(ref p, 293); long v294 = At(ref p, 294); long v295 = At(ref p, 295);
        long v296 = At(ref p, 296); long v297 = At(ref p, 297); long v298 = At(ref p, 298); long v299 = At(ref p, 299);
        long s = v0 - v1 + v2 - v3 + v4 - v5 + v6 - v7 + v8 - v9 + v10 - v11 + v12 - v13
               + v14 - v15 + v16 - v17 + v18 - v19 + v20 - v21 + v22 - v23 + v24 - v25 + v26 - v27
               + v28 - v29 + v30 - v31 + v32 - v33 + v34 - v35 + v36 - v37 + v38 - v39 + v40 - v41
               + v42 - v43 + v44 - v45 + v46 - v47 + v48 - v49 + v50 - v51 + v52 - v53 + v54 - v55
               + v56 - v57 + v58 - v59 + v60 - v61 + v62 - v63 + v64 - v65 + v66 - v67 + v68 - v69
               + v70 - v71 + v72 - v73 + v74 - v75 + v76 - v77 + v78 - v79 + v80 - v81 + v82 - v83
               + v84 - v85 + v86 - v87 + v88 - v89 + v90 - v91 + v92 - v93 + v94 - v95 + v96 - v97
               + v98 - v99 + v100 - v101 + v102 - v103 + v104 - v105 + v106 - v107 + v108 - v109 + v110 - v111
               + v112 - v113 + v114 - v115 + v116 - v117 + v118 - v119 + v120 - v121 + v122 - v123 + v124 - v125
               + v126 - v127 + v128 - v129 + v130 - v131 + v132 - v133 + v134 - v135 + v136 - v137 + v138 - v139
               + v140 - v141 + v142 - v143 + v144 - v145 + v146 - v147 + v148 - v149 + v150 - v151 + v152 - v153
               + v154 - v155 + v156 - v157 + v158 - v159 + v160 - v161 + v162 - v163 + v164 - v165 + v166 - v167
               + v168 - v169 + v170 - v171 + v172 - v173 + v174 - v175 + v176 - v177 + v178 - v179 + v180 - v181
               + v182 - v183 + v184 - v185 + v186 - v187 + v188 - v189 + v190 - v191 + v192 - v193 + v194 - v195
               + v196 - v197 + v198 - v199 + v200 - v201 + v202 - v203 + v204 - v205 + v206 - v207 + v208 - v209
               + v210 - v211 + v212 - v213 + v214 - v215 + v216 - v217 + v218 - v219 + v220 - v221 + v222 - v223
               + v224 - v225 + v226 - v227 + v228 - v229 + v230 - v231 + v232 - v233 + v234 - v235 + v236 - v237
               + v238 - v239 + v240 - v241 + v242 - v243 + v244 - v245 + v246 - v247 + v248 - v249 + v250 - v251
               + v252 - v253 + v254 - v255 + v256 - v257 + v258 - v259 + v260 - v261 + v262 - v263 + v264 - v265
               + v266 - v267 + v268 - v269 + v270 - v271 + v272 - v273 + v274 - v275 + v276 - v277 + v278 - v279
               + v280 - v281 + v282 - v283 + v284 - v285 + v286 - v287 + v288 - v289 + v290 - v291 + v292 - v293
               + v294 - v295 + v296 - v297 + v298 - v299;
        if (a < 0) Throw.Two(71, s);
        return s + a;
    }

    // A large value filled by a call through a pointer to it. The frame
    // has to stay, so the call that does not return stays a call.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static T MakeBig<T>(long a, long b) where T : struct
    {
        T r = default;
        ref long q = ref Unsafe.As<T, long>(ref r);
        q = a;
        Unsafe.Add(ref q, Unsafe.SizeOf<T>() / 8 - 1) = b;
        return r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Big<T>(long a, long b) where T : struct
    {
        T s = MakeBig<T>(a, b);
        ref long q = ref Unsafe.As<T, long>(ref s);
        long last = Unsafe.Add(ref q, Unsafe.SizeOf<T>() / 8 - 1);
        if (q < 0) Throw.Code((int)last);
        return q * 10 + last;
    }

    // The same with values kept in callee-saved registers across the call.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long BigLive<T>(long a, long b) where T : struct
    {
        long v1 = a + 1, v2 = a * 3, v3 = b - 7, v4 = a ^ b, v5 = b << 2, v6 = a - b;
        T s = MakeBig<T>(a, b);
        ref long q = ref Unsafe.As<T, long>(ref s);
        long last = Unsafe.Add(ref q, Unsafe.SizeOf<T>() / 8 - 1);
        if (q < 0) Throw.Two((int)last, v1 + v2 + v3 + v4 + v5 + v6);
        return q + last + v1 + v2 + v3 + v4 + v5 + v6;
    }

    // ---- helpers of other kinds ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long ManyArgs(long a, long b)
    {
        long x = a + b;
        if (a < 0) Throw.Many(1, 2, 3, 4, 5, 6, 7, 8, b, x);
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Value(long a) => a < 0 ? Throw.Value(41) + 5 : a;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long ValueAsArgument(long a) => a < 0 ? Op(a + Throw.Value(42)) : a;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long BigValue(long a)
    {
        if (a < 0)
        {
            S40 s = Throw.Big(43);
            return s.A + s.E;
        }
        return a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Generic(long a, string s)
    {
        if (a == 1) Throw.Of(s, 44);
        if (a == 2) Throw.Of(123456, 45);
        if (a == 3) Gen<string>.Fail(s, 46);
        if (a == 4) Gen<long>.Fail(a, 47);
        return a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long StructArg(long a)
    {
        S40 s;
        s.A = a; s.B = 2; s.C = 3; s.D = 4; s.E = 5;
        if (a < 0) Throw.Struct(s, 48);
        return s.A + s.B + s.C + s.D + s.E;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Instance(Thrower t, long a)
    {
        if (a < 0) t.Fail(49);
        return a + t.K;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static S40 ReturnsStruct(long a)
    {
        if (a < 0) Throw.Code(50);
        S40 r;
        r.A = a; r.B = a + 1; r.C = a + 2; r.D = a + 3; r.E = a + 4;
        return r;
    }

    // ---- the frame has to stay ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Fill(out long v, long a) { v = a * 2; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long AddressTaken(long a)
    {
        long tmp;
        Fill(out tmp, a);
        if (tmp < 0) Throw.Two(51, tmp);
        return tmp;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long StackAlloc(long a, int n)
    {
        Span<long> s = stackalloc long[n];
        s[0] = a;
        s[n - 1] += a;
        if (a < 0) Throw.Code(52);
        return s[0] + s[n - 1];
    }

    // The handler reads what was stored before the call.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long InTry(long a)
    {
        long x = a;
        try
        {
            x = a * 2;
            if (a < 0) Throw.Code(53);
            x += 1;
        }
        catch (E e)
        {
            return x * 1000 - e.Code;
        }
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long InTryFinally(long a)
    {
        long x = a;
        try
        {
            x = a * 2;
            if (a < 0) Throw.Code(54);
            x += 1;
        }
        finally
        {
            counter += x;
        }
        return x;
    }

    // The call is in the handler.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long InCatch(long a)
    {
        long x = a + 5;
        try
        {
            Guard(a, 55);
        }
        catch (E e)
        {
            x += e.Code;
            Throw.Two(56, x);
        }
        return x;
    }

    [MethodImpl(MethodImplOptions.Synchronized | MethodImplOptions.NoInlining)]
    static long Synchronized(long a)
    {
        if (a < 0) Throw.Code(57);
        return a + 1;
    }

    // ---- the target may return after all ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Virtual(Thrower t, long a)
    {
        long x = a * 3;
        t.MayFail(60);
        return x + t.Seen;
    }

    // The exact type is known here, so the call is not virtual any more.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long VirtualKnown(long a)
    {
        Quiet q = new Quiet();
        long x = a * 3;
        q.MayFail(61);
        return x + q.Seen;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Overridden(Lenient l, long a)
    {
        long x = a * 3;
        l.Check(62);
        return x + l.Seen;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long OverriddenKnown(long a)
    {
        Strict s = new Strict();
        long x = a * 3;
        s.Check(63);
        return x + s.Seen;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Interface(IFail f, long a)
    {
        long x = a * 3;
        f.Fail(64);
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Delegate(Action<int> f, long a)
    {
        long x = a * 3;
        f(65);
        return x;
    }

    // ---- the groups ----

    static int Guards()
    {
        int f = 0;
        f += Eq("guard", Try(() => Guard(4, 1)), 5);
        f += Eq("guard throws", Try(() => Guard(-4, 1)), Thrown + 100);
        f += Eq("guard live", Try(() => GuardLive(4, 5)), 21);
        f += Eq("guard live throws", Try(() => GuardLive(4, -5)), Thrown + 200);
        f += Eq("guard plain", Try(() => GuardLivePlain(4, 5)), 21);
        f += Eq("guard plain throws", Try(() => GuardLivePlain(4, -5)), Thrown + 200);
        f += Eq("guard inlinable", Try(() => GuardInlinable(4, 5)), 21);
        f += Eq("guard inlinable throws", Try(() => GuardInlinable(4, -5)), Thrown + 300);
        f += Eq("guards", Try(() => Guards(1, 2, 3)), 321);
        f += Eq("guards 1", Try(() => Guards(-1, 2, 3)), Thrown + 1100);
        f += Eq("guards 2", Try(() => Guards(1, -2, 3)), Thrown + 1300);
        f += Eq("guards 3", Try(() => Guards(1, 2, -3)), Thrown + 1300);
        f += Eq("stack args", Try(() => StackArgs(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11)), 39);
        f += Eq("stack args throw", Try(() => StackArgs(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, -11)), Thrown + 1900);
        f += Eq("loop", Try(() => Loop(10, 100)), 55);
        f += Eq("loop throws", Try(() => Loop(10, 20)), Thrown + 600);
        f += Eq("switch 0", Try(() => Switch(0, 7)), Thrown + 2000);
        f += Eq("switch 1", Try(() => Switch(1, 7)), 8);
        f += Eq("switch 2", Try(() => Switch(2, 7)), Thrown + 2800);
        f += Eq("switch 3", Try(() => Switch(3, 7)), 14);
        f += Eq("switch 4", Try(() => Switch(4, 7)), Thrown + 2500);
        f += Eq("switch 5", Try(() => Switch(5, 7)), Thrown + 2300);
        f += Eq("switch 6", Try(() => Switch(6, 7)), 7);
        counter = 0;
        f += Eq("always", Try(() => { Always(30); return 0; }), Thrown + 3000);
        f += Eq("always ran", counter, 1);
        f += Eq("requires", Try(() => Requires(5, 6)), 30);
        f += Eq("requires 1", Try(() => Requires(-5, 6)), Thrown + 3100);
        f += Eq("requires 2", Try(() => Requires(5, -6)), Thrown + 3200);
        f += Eq("requires 3", Try(() => Requires(50, 60)), Thrown + 3300);
        f += Eq("deep", Try(() => Deep(100, 4)), 105);
        f += Eq("deep throws", Try(() => Deep(100, -4)), Thrown + 3400);
        return f;
    }

    static readonly long[] words = new long[320];

    static int Registers()
    {
        int f = 0;
        f += Eq("after call", Try(() => AfterCall(4, 5)), 18);
        f += Eq("after call throws", Try(() => AfterCall(-4, 5)), Thrown + 800);
        f += Eq("twelve", Try(() => Twelve(10, 3)), 12 * 10 + 66 + 3);
        f += Eq("twelve throws", Try(() => Twelve(10, -3)), Thrown + 3100);
        f += Eq("live 14", Try(() => Live14(ref words[0], 2)), 2028);
        f += Eq("live 14 throws", Try(() => Live14(ref words[0], -2)), Thrown + 209600);
        f += Eq("live 18", Try(() => Live18(ref words[0], 2)), 3072);
        f += Eq("live 18 throws", Try(() => Live18(ref words[0], -2)), Thrown + 314000);
        f += Eq("live 22", Try(() => Live22(ref words[0], 2)), 4969);
        f += Eq("live 22 throws", Try(() => Live22(ref words[0], -2)), Thrown + 503700);
        f += Eq("live 26", Try(() => Live26(ref words[0], 2)), 7523);
        f += Eq("live 26 throws", Try(() => Live26(ref words[0], -2)), Thrown + 759100);
        long sum = 0;
        for (int i = 0; i < 100; i++)
            sum += Try(() => Twelve(i, -1)) - Thrown;
        f += Eq("repeated throws", sum, 100 * 1100 + 2 * 4950 * 100);
        return f;
    }

    static int Large()
    {
        int f = 0;
        f += Eq("wide 266", Try(() => Wide266(ref words[0], 2)), -131);
        f += Eq("wide 266 throws", Try(() => Wide266(ref words[0], -2)), Thrown - 6200);
        f += Eq("wide 267", Try(() => Wide267(ref words[0], 2)), 136);
        f += Eq("wide 267 throws", Try(() => Wide267(ref words[0], -2)), Thrown + 20500);
        f += Eq("wide 268", Try(() => Wide268(ref words[0], 2)), -132);
        f += Eq("wide 268 throws", Try(() => Wide268(ref words[0], -2)), Thrown - 6300);
        f += Eq("wide 300", Try(() => Wide300(ref words[0], 2)), -148);
        f += Eq("wide 300 throws", Try(() => Wide300(ref words[0], -2)), Thrown - 7900);
        f += Eq("large 16", Try(() => Big<B16>(3, 4)) + Try(() => BigLive<B16>(3, 4)), 73);
        f += Eq("large 16 throws", Try(() => Big<B16>(-3, 4)) + Try(() => BigLive<B16>(-3, 4)), 2 * Thrown - 400);
        f += Eq("large 2024", Try(() => Big<B2024>(3, 4)) + Try(() => BigLive<B2024>(3, 4)), 73);
        f += Eq("large 2024 throws", Try(() => Big<B2024>(-3, 4)) + Try(() => BigLive<B2024>(-3, 4)), 2 * Thrown - 400);
        f += Eq("large 2032", Try(() => Big<B2032>(3, 4)) + Try(() => BigLive<B2032>(3, 4)), 73);
        f += Eq("large 2032 throws", Try(() => Big<B2032>(-3, 4)) + Try(() => BigLive<B2032>(-3, 4)), 2 * Thrown - 400);
        f += Eq("large 2040", Try(() => Big<B2040>(3, 4)) + Try(() => BigLive<B2040>(3, 4)), 73);
        f += Eq("large 2040 throws", Try(() => Big<B2040>(-3, 4)) + Try(() => BigLive<B2040>(-3, 4)), 2 * Thrown - 400);
        f += Eq("large 2048", Try(() => Big<B2048>(3, 4)) + Try(() => BigLive<B2048>(3, 4)), 73);
        f += Eq("large 2048 throws", Try(() => Big<B2048>(-3, 4)) + Try(() => BigLive<B2048>(-3, 4)), 2 * Thrown - 400);
        f += Eq("large 2056", Try(() => Big<B2056>(3, 4)) + Try(() => BigLive<B2056>(3, 4)), 73);
        f += Eq("large 2056 throws", Try(() => Big<B2056>(-3, 4)) + Try(() => BigLive<B2056>(-3, 4)), 2 * Thrown - 400);
        f += Eq("large 4096", Try(() => Big<B4096>(3, 4)) + Try(() => BigLive<B4096>(3, 4)), 73);
        f += Eq("large 4096 throws", Try(() => Big<B4096>(-3, 4)) + Try(() => BigLive<B4096>(-3, 4)), 2 * Thrown - 400);
        return f;
    }

    static int Helpers()
    {
        int f = 0;
        f += Eq("many args", Try(() => ManyArgs(4, 5)), 9);
        f += Eq("many args throw", Try(() => ManyArgs(-4, 5)), Thrown + (36 + 500 + 1000) * 100);
        f += Eq("value", Try(() => Value(4)), 4);
        f += Eq("value throws", Try(() => Value(-4)), Thrown + 4100);
        f += Eq("value as argument", Try(() => ValueAsArgument(4)), 4);
        f += Eq("value as argument throws", Try(() => ValueAsArgument(-4)), Thrown + 4200);
        f += Eq("big value", Try(() => BigValue(4)), 4);
        f += Eq("big value throws", Try(() => BigValue(-4)), Thrown + 4300);
        f += Eq("generic 0", Try(() => Generic(0, "abc")), 0);
        f += Eq("generic 1", Try(() => Generic(1, "abc")), Thrown + 4700);
        f += Eq("generic 2", Try(() => Generic(2, "abc")), Thrown + 5100);
        f += Eq("generic 3", Try(() => Generic(3, "abc")), Thrown + 4700);
        f += Eq("generic 4", Try(() => Generic(4, "abc")), Thrown + 4800);
        f += Eq("struct argument", Try(() => StructArg(4)), 18);
        f += Eq("struct argument throws", Try(() => StructArg(-4)), Thrown + 4900);
        Thrower t = new Thrower { K = 3 };
        f += Eq("instance", Try(() => Instance(t, 4)), 7);
        f += Eq("instance throws", Try(() => Instance(t, -4)), Thrown + 5200);
        f += Eq("returns struct", Try(() => { S40 s = ReturnsStruct(4); return s.A + s.E; }), 12);
        f += Eq("returns struct throws", Try(() => { S40 s = ReturnsStruct(-4); return s.A + s.E; }), Thrown + 5000);
        Acc acc = new Acc { V = 10 };
        f += Eq("struct method", acc.Add(5), 15);
        long got = 0;
        try { acc.Add(-5); } catch (E e) { got = e.Code; }
        f += Eq("struct method throws", got * 100 + acc.V, 2615);
        return f;
    }

    static int Frames()
    {
        int f = 0;
        f += Eq("address taken", Try(() => AddressTaken(4)), 8);
        f += Eq("address taken throws", Try(() => AddressTaken(-4)), Thrown + 4300);
        f += Eq("stackalloc", Try(() => StackAlloc(4, 8)), 8);
        f += Eq("stackalloc throws", Try(() => StackAlloc(-4, 8)), Thrown + 5200);
        f += Eq("in try", InTry(4), 9);
        f += Eq("in try throws", InTry(-4), -8053);
        counter = 0;
        f += Eq("in try finally", Try(() => InTryFinally(4)), 9);
        f += Eq("in try finally throws", Try(() => InTryFinally(-4)), Thrown + 5400);
        f += Eq("finally ran", counter, 9 - 8);
        f += Eq("in catch", Try(() => InCatch(4)), 9);
        f += Eq("in catch throws", Try(() => InCatch(-4)), Thrown + (56 + 1 + 55) * 100);
        f += Eq("synchronized", Try(() => Synchronized(4)), 5);
        f += Eq("synchronized throws", Try(() => Synchronized(-4)), Thrown + 5700);
        f += Eq("synchronized again", Try(() => Synchronized(6)), 7);
        return f;
    }

    static int Returning()
    {
        int f = 0;
        Thrower loud = new Thrower { K = 1 };
        Thrower quiet = new Quiet();
        f += Eq("virtual returns", Try(() => Virtual(quiet, 5)), 15 + 60);
        f += Eq("virtual throws", Try(() => Virtual(loud, 5)), Thrown + 6100);
        f += Eq("virtual known returns", Try(() => VirtualKnown(5)), 15 + 61);
        Lenient lenient = new Lenient(), strict = new Strict();
        f += Eq("overridden returns", Try(() => Overridden(lenient, 5)), 15 + 62);
        f += Eq("overridden throws", Try(() => Overridden(strict, 5)), Thrown + 6200);
        f += Eq("overridden known throws", Try(() => OverriddenKnown(5)), Thrown + 6300);
        FakeFail fake = new FakeFail();
        f += Eq("interface returns", Try(() => Interface(fake, 5)), 15);
        f += Eq("interface saw", fake.Seen, 64);
        f += Eq("interface throws", Try(() => Interface(new RealFail(), 5)), Thrown + 6400);
        f += Eq("delegate throws", Try(() => Delegate(Throw.Code, 5)), Thrown + 6500);
        f += Eq("delegate returns", Try(() => Delegate(c => counter = c, 5)), 15);
        return f;
    }

    static int Main()
    {
        for (int i = 0; i < words.Length; i++) words[i] = i + 1;

        Group("guards", Guards);
        Group("registers at the jump", Registers);
        Group("large frames", Large);
        Group("helpers", Helpers);
        Group("frames that stay", Frames);
        Group("targets that return", Returning);

        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails;
    }
}
