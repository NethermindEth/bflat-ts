// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Methods that end in a tail call and do not save the return address.
//
// A call in the last position of a method becomes a jump: the method pops
// its frame, restores the registers it saved and jumps, and the callee
// returns to the caller of the method. A method whose only calls are of this
// kind never writes RA, so on a zkVM guest it does not save it, has no slot
// for it and often has no frame at all. What can go wrong is everything the
// epilog does before the jump: the frame has to be gone, every callee-saved
// register has to be back, and the registers that carry the arguments and
// the target of an indirect jump must survive that.
//
// The cases:
//   - a direct tail call with the arguments passed on, rotated, computed;
//   - arguments on the stack: the method writes them over its own incoming
//     ones, in another order;
//   - a tail call on every branch of an if and of a switch;
//   - a normal call first and a tail call last, with values kept in
//     callee-saved registers between them: RA is saved here and restored
//     before the jump;
//   - only tail calls but so many values alive that callee-saved registers
//     are used: they are saved in a frame without a slot for RA;
//   - a frame of more than 2040 bytes under the jump, which is popped in
//     two steps, also with the target of the jump in a register;
//   - indirect targets: a delegate, a virtual method, an interface method;
//   - results in two registers and through a buffer of the caller;
//   - a callee that throws: the method that jumped to it is not on the
//     stack any more, and the exception goes to a handler above it;
//   - two methods that tail call each other many times.
//
// The JIT makes a tail call only from a method that may be inlined, so the
// methods under test carry no NoInlining; they are reached through
// delegates, which keeps them from being inlined into the test itself.
//
// Every group of cases runs under a caller that keeps twelve values across
// the call, one for every callee-saved register, and checks them after it.
//
// Each group prints one line, and one more for each case that failed; the
// run ends with ALL PASS or a FAIL count, and the guest returns the number
// of failures.
using System;
using System.Runtime.CompilerServices;

class E : Exception { public int Code; public E(int c) : base("e") { Code = c; } }

struct S16 { public long A, B; }
struct S40 { public long A, B, C, D, E; }

delegate long F2(long a, long b);
delegate long F8(long a1, long a2, long a3, long a4, long a5, long a6, long a7, long a8);
delegate long F11(long a1, long a2, long a3, long a4, long a5, long a6, long a7, long a8, long s1, long s2, long s3);
delegate long FRef(ref long p, long a, long b);
delegate long FDel(F2 f, long a, long b);
delegate long FRefDel(F2 f, ref long p, long a, long b);
delegate long FObj(Shape s, long a);
delegate long FItf(IShape s, long a);
delegate S16 FPair(long a, long b);
delegate S40 FFive(long a, long b);
delegate void FVoid(Cell c, long v);
delegate long FStruct(S40 s, long a);
delegate long FStr(string s, long a);

class Cell { public long V; }

abstract class Shape
{
    public long K;
    public abstract long Area(long a);

    // An instance method that ends in a call of another one.
    public long Twice(long a) => Scaled(a, 2);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public long Scaled(long a, long n) => a * n + K;
}

class Square : Shape
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override long Area(long a) => a * a + K;
}

class Line : Shape
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override long Area(long a) => a + K;
}

interface IShape { long Edge(long a); }

class Tri : IShape
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public long Edge(long a) => a * 3;
}

class Hex : IShape
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public long Edge(long a) => a * 6;
}

static class Gen<T>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Len(T x, long a) => (x == null ? 0 : x.ToString().Length) + a;

    public static long Tail(T x, long a) => Len(x, a + 1);
}

class Program
{
    static int fails;

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

    // ---- the callees ----

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Callee2(long a, long b) => a * 10 + b;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Callee8(long a1, long a2, long a3, long a4, long a5, long a6, long a7, long a8) =>
        a1 + a2 * 2 + a3 * 3 + a4 * 4 + a5 * 5 + a6 * 6 + a7 * 7 + a8 * 8;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Callee11(long a1, long a2, long a3, long a4, long a5, long a6, long a7, long a8,
                         long s1, long s2, long s3) =>
        a1 + a2 * 2 + a3 * 3 + a4 * 4 + a5 * 5 + a6 * 6 + a7 * 7 + a8 * 8 + s1 * 100 + s2 * 1000 + s3 * 10000;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long ThrowIfNeg(long a, long b)
    {
        if (a < 0) throw new E((int)b);
        return a + b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static S16 MakePair(long a, long b)
    {
        S16 r;
        r.A = a + 1;
        r.B = b + 2;
        return r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static S40 MakeFive(long a, long b)
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
    static void StoreTo(Cell c, long v) { c.V = v; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Sum40(S40 s, long a) => s.A + s.B * 2 + s.C * 3 + s.D * 4 + s.E * 5 + a;

    // ---- the methods under test: no NoInlining, reached through delegates ----

    static long Direct(long a, long b) => Callee2(b, a + 1);

    static long Rotate8(long a1, long a2, long a3, long a4, long a5, long a6, long a7, long a8) =>
        Callee8(a8, a1, a2, a3, a4, a5, a6, a7);

    static long Reverse8(long a1, long a2, long a3, long a4, long a5, long a6, long a7, long a8) =>
        Callee8(a8, a7, a6, a5, a4, a3, a2, a1);

    static long Widen(long a, long b) => Callee8(a, b, a + b, a - b, a * b, a ^ b, a | b, a & b);

    // Outgoing stack arguments go where the incoming ones are.
    static long StackSame(long a1, long a2, long a3, long a4, long a5, long a6, long a7, long a8,
                          long s1, long s2, long s3) =>
        Callee11(a1, a2, a3, a4, a5, a6, a7, a8, s1, s2, s3);

    static long StackRotate(long a1, long a2, long a3, long a4, long a5, long a6, long a7, long a8,
                            long s1, long s2, long s3) =>
        Callee11(s1, a1, a2, a3, a4, a5, a6, a7, s3, a8, s2);

    static long StackSwap(long a1, long a2, long a3, long a4, long a5, long a6, long a7, long a8,
                          long s1, long s2, long s3) =>
        Callee11(a1, a2, a3, a4, a5, a6, a7, a8, s3, s2 + 1, s1);

    // Fewer stack arguments out than in.
    static long StackToRegs(long a1, long a2, long a3, long a4, long a5, long a6, long a7, long a8,
                            long s1, long s2, long s3) =>
        Callee8(s3, s2, s1, a8, a7, a6, a5, a4);

    // More stack arguments out than in: there is no room for a jump.
    static long RegsToStack(long a, long b) => Callee11(a, b, a, b, a, b, a, b, a + 1, b + 1, a + b);

    static long Branch(long a, long b) => a > 0 ? Callee2(a, b) : Callee8(b, a, 1, 1, 1, 1, 1, 1);

    static long Switch(long a, long b)
    {
        switch (a)
        {
            case 0: return Callee2(b, 1);
            case 1: return Callee2(1, b);
            case 2: return Callee8(b, b, b, b, 0, 0, 0, 0);
            case 3: return Callee2(b, b);
            case 4: return ThrowIfNeg(b, 4);
            default: return b;
        }
    }

    // A normal call, then the tail call: RA is written and has to be saved.
    static long Mixed(long a, long b)
    {
        long t = Op(a + 1);
        return Callee2(t, b);
    }

    // The same with values kept across the normal call.
    static long MixedLive(long a, long b)
    {
        long v1 = a + 1, v2 = a * 3, v3 = b - 7, v4 = a ^ b, v5 = b << 2, v6 = a - b;
        long t = Op(a);
        return Callee8(t, v1, v2, v3, v4, v5, v6, b);
    }

    static long MixedThrow(long a, long b)
    {
        long v1 = a + 1, v2 = a * 3, v3 = b - 7, v4 = a ^ b;
        long t = Op(v1 + v2);
        return ThrowIfNeg(a, t + v1 + v2 + v3 + v4);
    }

    static long Throw(long a, long b) => ThrowIfNeg(a, b + 1);

    static long ViaDelegate(F2 f, long a, long b) => f(b, a);

    static long ViaVirtual(Shape s, long a) => s.Area(a + 1);

    static long ViaInterface(IShape s, long a) => s.Edge(a + 1);

    static long ViaInstance(Shape s, long a) => s.Twice(a);

    static S16 Pair(long a, long b) => MakePair(b, a);

    static S40 Five(long a, long b) => MakeFive(b, a);

    static void Void(Cell c, long v) => StoreTo(c, v + 1);

    static long Struct(S40 s, long a) => Sum40(s, a + 1);

    // Two methods that call each other in the last position.
    static long Ping(long n, long acc) => n == 0 ? acc : Pong(n - 1, acc + 3);

    static long Pong(long n, long acc) => n == 0 ? -acc : Ping(n - 1, acc - 1);

    // A method that calls itself in the last position.
    static long Gcd(long a, long b) => b == 0 ? a : Gcd(b, a % b);

    // Many values alive and no call but the last one.
    static long Live13(ref long p, long a, long b)
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
        return Callee8(s, x, a, b, v0, v1, v2, v3);
    }

    static long Live16(ref long p, long a, long b)
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
        return Callee8(s, x, a, b, v0, v1, v2, v3);
    }

    static long Live19(ref long p, long a, long b)
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
        return Callee8(s, x, a, b, v0, v1, v2, v3);
    }

    static long Live22(ref long p, long a, long b)
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
        return Callee8(s, x, a, b, v0, v1, v2, v3);
    }

    static long Live25(ref long p, long a, long b)
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
        return Callee8(s, x, a, b, v0, v1, v2, v3);
    }

    static long Live28(ref long p, long a, long b)
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
        long v27 = Unsafe.Add(ref p, 27);
        long s = v0 * 1 + v1 * 2 + v2 * 3 + v3 * 4 + v4 * 5 + v5 * 6 + v6 * 7 + v7 * 8
               + v8 * 9 + v9 * 10 + v10 * 11 + v11 * 12 + v12 * 13 + v13 * 14 + v14 * 15 + v15 * 16
               + v16 * 17 + v17 * 18 + v18 * 19 + v19 * 20 + v20 * 21 + v21 * 22 + v22 * 23 + v23 * 24
               + v24 * 25 + v25 * 26 + v26 * 27 + v27 * 28;
        long x = (v27 << 0) ^ (v26 << 1) ^ (v25 << 2) ^ (v24 << 3) ^ (v23 << 4) ^ (v22 << 5) ^ (v21 << 6) ^ (v20 << 0)
               ^ (v19 << 1) ^ (v18 << 2) ^ (v17 << 3) ^ (v16 << 4) ^ (v15 << 5) ^ (v14 << 6) ^ (v13 << 0) ^ (v12 << 1)
               ^ (v11 << 2) ^ (v10 << 3) ^ (v9 << 4) ^ (v8 << 5) ^ (v7 << 6) ^ (v6 << 0) ^ (v5 << 1) ^ (v4 << 2)
               ^ (v3 << 3) ^ (v2 << 4) ^ (v1 << 5) ^ (v0 << 6);
        return Callee8(s, x, a, b, v0, v1, v2, v3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static long At(ref long p, int i) => Unsafe.Add(ref p, i);

    // So many values alive that most of them are kept in the frame, which
    // gets larger than 2040 bytes and is allocated in two steps.
    static long Wide264(ref long p, long a, long b)
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
               + v252 - v253 + v254 - v255 + v256 - v257 + v258 - v259 + v260 - v261 + v262 - v263;
        return Callee2(s + a, b);
    }

    static long Wide265(ref long p, long a, long b)
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
        long v264 = At(ref p, 264);
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
               + v252 - v253 + v254 - v255 + v256 - v257 + v258 - v259 + v260 - v261 + v262 - v263 + v264;
        return Callee2(s + a, b);
    }

    static long Wide266(ref long p, long a, long b)
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
        return Callee2(s + a, b);
    }

    // The same with the target of the jump in a register.
    static long WideVia268(F2 f, ref long p, long a, long b)
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
        return f(s + a, b);
    }

    static long WideVia300(F2 f, ref long p, long a, long b)
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
        return f(s + a, b);
    }

    // ---- the delegates: set at run time, so the targets are not known ----

    static F2 direct, widen, regsToStack, branch, switch_, mixed, mixedLive, mixedThrow, throw_, ping, gcd, callee2;
    static F8 rotate8, reverse8;
    static F11 stackSame, stackRotate, stackSwap, stackToRegs;
    static FDel viaDelegate;
    static FObj viaVirtual, viaInstance;
    static FItf viaInterface;
    static FPair pair;
    static FFive five;
    static FVoid void_;
    static FStruct struct_;
    static FStr genString;
    static FRef[] live;
    static FRef[] wide;
    static FRefDel[] wideVia;
    static readonly long[] words = new long[320];

    static void Setup()
    {
        direct = Direct; widen = Widen; regsToStack = RegsToStack; branch = Branch; switch_ = Switch;
        mixed = Mixed; mixedLive = MixedLive; mixedThrow = MixedThrow; throw_ = Throw; ping = Ping; gcd = Gcd;
        callee2 = Callee2;
        rotate8 = Rotate8; reverse8 = Reverse8;
        stackSame = StackSame; stackRotate = StackRotate; stackSwap = StackSwap; stackToRegs = StackToRegs;
        viaDelegate = ViaDelegate; viaVirtual = ViaVirtual; viaInstance = ViaInstance; viaInterface = ViaInterface;
        pair = Pair; five = Five; void_ = Void; struct_ = Struct; genString = Gen<string>.Tail;
        live = new FRef[] { Live13, Live16, Live19, Live22, Live25, Live28 };
        wide = new FRef[] { Wide264, Wide265, Wide266 };
        wideVia = new FRefDel[] { WideVia268, WideVia300 };
        for (int i = 0; i < words.Length; i++) words[i] = i + 1;
    }

    // ---- the groups ----

    static int Direct()
    {
        int f = 0;
        f += Eq("direct", direct(3, 4), 44);
        f += Eq("rotate", rotate8(1, 2, 3, 4, 5, 6, 7, 8), 8 + 2 + 6 + 12 + 20 + 30 + 42 + 56);
        f += Eq("reverse", reverse8(1, 2, 3, 4, 5, 6, 7, 8), 8 + 14 + 18 + 20 + 20 + 18 + 14 + 8);
        f += Eq("widen", widen(12, 5), 12 + 10 + 51 + 28 + 300 + 54 + 91 + 32);
        f += Eq("branch +", branch(2, 9), 29);
        f += Eq("branch -", branch(-2, 9), 9 - 4 + 3 + 4 + 5 + 6 + 7 + 8);
        f += Eq("switch 0", switch_(0, 6), 61);
        f += Eq("switch 1", switch_(1, 6), 16);
        f += Eq("switch 2", switch_(2, 6), 60);
        f += Eq("switch 3", switch_(3, 6), 66);
        f += Eq("switch 4", switch_(4, 6), 10);
        f += Eq("switch 5", switch_(5, 6), 6);
        f += Eq("gcd", gcd(1071, 462), 21);
        f += Eq("ping", ping(1500, 0), 1500);
        f += Eq("ping odd", ping(1501, 0), -1503);
        f += Eq("generic", genString("abcd", 5), 10);
        return f;
    }

    static int Stack()
    {
        int f = 0;
        f += Eq("stack same", stackSame(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), 204 + 900 + 10000 + 110000);
        f += Eq("stack rotate", stackRotate(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11),
                9 + 2 + 6 + 12 + 20 + 30 + 42 + 56 + 1100 + 8000 + 100000);
        f += Eq("stack swap", stackSwap(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), 204 + 1100 + 11000 + 90000);
        f += Eq("stack to regs", stackToRegs(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11),
                11 + 20 + 27 + 32 + 35 + 36 + 35 + 32);
        f += Eq("regs to stack", regsToStack(2, 3), 2 + 6 + 6 + 12 + 10 + 18 + 14 + 24 + 300 + 4000 + 50000);
        return f;
    }

    static int MixedCalls()
    {
        int f = 0;
        f += Eq("mixed", mixed(3, 4), 44);
        f += Eq("mixed live", mixedLive(10, 3), 10 + 22 + 90 - 16 + 45 + 72 + 49 + 24);
        f += Eq("mixed no throw", mixedThrow(2, 9), 2 + 9 + 3 + 6 + 2 + 11);
        return f;
    }

    static int Indirect()
    {
        int f = 0;
        Shape sq = new Square { K = 5 }, ln = new Line { K = 7 };
        IShape tri = new Tri(), hex = new Hex();
        f += Eq("delegate", viaDelegate(callee2, 3, 4), 43);
        f += Eq("delegate to tail", viaDelegate(direct, 3, 4), 35);
        f += Eq("virtual 1", viaVirtual(sq, 6), 54);
        f += Eq("virtual 2", viaVirtual(ln, 6), 14);
        f += Eq("interface 1", viaInterface(tri, 6), 21);
        f += Eq("interface 2", viaInterface(hex, 6), 42);
        f += Eq("instance", viaInstance(sq, 6), 17);
        S16 p = pair(10, 20);
        f += Eq("pair", p.A * 100 + p.B, 2112);
        S40 v = five(4, 9);
        f += Eq("five", v.A + v.B * 10 + v.C * 100 + v.D * 1000 + v.E * 10000, 9 + 40 + 1300 + 5000 + 360000);
        Cell c = new Cell();
        void_(c, 41);
        f += Eq("void", c.V, 42);
        S40 s = new S40 { A = 1, B = 2, C = 3, D = 4, E = 5 };
        f += Eq("struct", struct_(s, 9), 55 + 10);
        f += Eq("struct kept", s.A + s.B + s.C + s.D + s.E, 15);
        return f;
    }

    // The handler is above the method that made the tail call.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Catch(F2 fn, long a, long b)
    {
        long k0 = Op(a + 100), k1 = Op(b + 200);
        try
        {
            return fn(a, b);
        }
        catch (E e)
        {
            return e.Code * 1000000 + k0 * 1000 + k1;
        }
    }

    static int Throws()
    {
        int f = 0;
        f += Eq("throw", Catch(throw_, -1, 8), 9 * 1000000 + 99 * 1000 + 208);
        f += Eq("no throw", Catch(throw_, 1, 8), 10);
        f += Eq("mixed throw", Catch(mixedThrow, -2, 9), (-7 - 1 - 6 + 2 - 9) * 1000000L + 98 * 1000 + 209);
        f += Eq("switch throw", Catch(switch_, 4, -6), 4 * 1000000 + 104 * 1000 + 194);
        f += Eq("delegate throw", Catch((a, b) => viaDelegate(throw_, b, a), -3, 5), 6 * 1000000 + 97 * 1000 + 205);
        long sum = 0;
        for (int i = 0; i < 50; i++) sum += Catch(i % 2 == 0 ? throw_ : mixedThrow, -1, i) / 1000000;
        f += Eq("repeated throws", sum, 625 - 25 * 13);
        return f;
    }

    static int Live()
    {
        int f = 0;
        ref long p = ref words[0];
        f += Eq("live 13", live[0](ref p, 3, 4), 1036);
        f += Eq("live 16", live[1](ref p, 3, 4), 3309);
        f += Eq("live 19", live[2](ref p, 3, 4), 4149);
        f += Eq("live 22", live[3](ref p, 3, 4), 6234);
        f += Eq("live 25", live[4](ref p, 3, 4), 7708);
        f += Eq("live 28", live[5](ref p, 3, 4), 10705);
        return f;
    }

    static int Wide()
    {
        int f = 0;
        ref long p = ref words[0];
        f += Eq("wide 264", wide[0](ref p, 3, 4), -1286);
        f += Eq("wide 265", wide[1](ref p, 3, 4), 1364);
        f += Eq("wide 266", wide[2](ref p, 3, 4), -1296);
        f += Eq("wide via 268", wideVia[0](callee2, ref p, 3, 4), -1306);
        f += Eq("wide via 300", wideVia[1](callee2, ref p, 3, 4), -1466);
        f += Eq("wide throw 268", Catch((a, b) => wideVia[0](throw_, ref words[0], a, b), 5, 7),
                8 * 1000000 + 105 * 1000 + 207);
        f += Eq("wide throw 300", Catch((a, b) => wideVia[1](throw_, ref words[0], a, b), 5, 7),
                8 * 1000000 + 105 * 1000 + 207);
        return f;
    }

    static int Main()
    {
        Setup();

        Group("direct", Direct);
        Group("stack arguments", Stack);
        Group("normal call first", MixedCalls);
        Group("indirect and structs", Indirect);
        Group("callee throws", Throws);
        Group("many live values", Live);
        Group("large frames", Wide);

        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAILED " + fails);
        return fails;
    }
}
