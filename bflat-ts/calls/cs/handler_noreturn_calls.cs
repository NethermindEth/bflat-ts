// SPDX-License-Identifier: MIT
// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Calls that do not return, made to a method with the handler convention.
//
// With JitZkHandlerConv a static method of the shape
//   (ref T, nuint, ref U, ref byte, nint, nint*, ref byte, ref byte) -> int
// takes its arguments in s1-s8 and saves none of them. A call that never
// returns and ends a throw block is made as a jump after the epilog of the
// caller (RiscV64ElideLeafRaSave). The two meet when a method that is not a
// handler makes such a call to a handler: its epilog restores s1-s8, the
// registers it has just put the arguments in. The arguments have to reach
// the callee whatever the caller is.
//
// The cases: the call from a method that keeps nothing in callee-saved
// registers and from one that keeps eight values across calls first, each
// taken on the path that throws and on the path that returns; the handler
// throws an exception that carries a number made of all eight arguments,
// and the caller of the caller, which keeps six values of its own, checks
// the number and its values after the catch.
//
// The run ends with ALL PASS or a FAIL count, and the guest returns the
// number of failures. A handler that reads wrong arguments may stop the
// guest instead. The package runs the program with the handler convention
// on every target.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

class E : Exception { public long Code; public E(long c) : base("e") { Code = c; } }

ref struct EvmStack { public long A; }
struct DispatchState { public long B; }

unsafe class Program
{
    static int fails;

    // The handler shape: (ref EvmStack, nuint, ref DispatchState, ref byte, nint, nint*, ref byte, ref byte) -> int
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Fail(ref EvmStack s, nuint pc, ref DispatchState d, ref byte code, nint gas, nint* tbl, ref byte m, ref byte e)
    {
        throw new E((long)pc * 1000000 + (long)gas * 10000 + s.A * 100 + d.B * 10 + code + m * 3 + e * 5 + (long)tbl[0] * 7);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Op(long v) => v;

    // Not a handler: its epilog restores the callee-saved registers it used.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Dispatch(ref EvmStack s, ref DispatchState d, ref byte b, nint* t, nuint pc)
    {
        if (pc != 42)
            return 0;
        return Fail(ref s, pc, ref d, ref b, 7, t, ref b, ref b);
    }

    // The same with values kept across a call first, so that the callee-saved registers hold other values.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int DispatchKeep(ref EvmStack s, ref DispatchState d, ref byte b, nint* t, nuint pc)
    {
        long k1 = Op(11), k2 = Op(12), k3 = Op(13), k4 = Op(14), k5 = Op(15), k6 = Op(16), k7 = Op(17), k8 = Op(18);
        if (Op(k1 + k2 + k3 + k4 + k5 + k6 + k7 + k8) != 116)
            return -1;
        if (pc != 42)
            return (int)(k1 + k8);
        return Fail(ref s, pc, ref d, ref b, 7, t, ref b, ref b);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Run(int which, nuint pc)
    {
        EvmStack s = default; s.A = 3;
        DispatchState d = default; d.B = 4;
        byte b = 9;
        nint cell = 2;
        long k1 = Op(101), k2 = Op(102), k3 = Op(103), k4 = Op(104), k5 = Op(105), k6 = Op(106);
        long r;
        try
        {
            r = which == 0 ? Dispatch(ref s, ref d, ref b, &cell, pc) : DispatchKeep(ref s, ref d, ref b, &cell, pc);
        }
        catch (E e)
        {
            r = e.Code;
        }
        if (k1 + k2 + k3 + k4 + k5 + k6 != 621) r = -777;
        return r;
    }

    static void Check(string name, long got, long want)
    {
        if (got != want) { Console.WriteLine("FAIL " + name + ": got " + got + ", want " + want); fails++; }
    }

    static int Main()
    {
        long want = 42L * 1000000 + 7 * 10000 + 3 * 100 + 4 * 10 + 9 + 9 * 3 + 9 * 5 + 2 * 7;
        Check("plain no throw", Run(0, 1), 0);
        Check("plain throw", Run(0, 42), want);
        Check("keep no throw", Run(1, 1), 29);
        Check("keep throw", Run(1, 42), want);
        Console.WriteLine(fails == 0 ? "ALL PASS" : "FAIL count " + fails);
        return fails;
    }
}
