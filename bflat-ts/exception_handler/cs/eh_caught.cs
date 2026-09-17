// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// The default build keeps the unwind tables, so a throw is dispatched the way
// it is anywhere else: the first pass searches the stack for a handler, the
// second unwinds to it. This is the half of the policy the ZkvmThrow tests do
// not cover - there the tables are gone and RhpThrowEx is diverted before any
// of that happens.
//
// Nested, so the search has to walk past a frame that does not handle the
// exception, and with a finally to prove the second pass actually unwinds
// rather than jumping straight to the handler.
using System;
using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Inner() => throw new InvalidOperationException("boom from managed code");

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Middle()
    {
        try
        {
            Inner();
        }
        finally
        {
            Console.WriteLine("[finally] middle");
        }
    }

    static int Main()
    {
        Console.WriteLine("before throw");
        try
        {
            Middle();
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine("[caught] " + ex.GetType().ToString() + ": " + ex.Message);
            return 0;
        }

        Console.WriteLine("[not reached] no exception was delivered");
        return 1;
    }
}
