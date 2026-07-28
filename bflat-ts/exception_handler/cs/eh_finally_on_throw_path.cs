// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// A finally in an intermediate frame must run while unwinding to an outer
// catch.
using System;

class Program
{
    static int FinallyRan;

    static void Middle()
    {
        try
        {
            throw new InvalidOperationException("through");
        }
        finally
        {
            FinallyRan = 1;
        }
    }

    static int Main()
    {
        try
        {
            Middle();
            return 1;
        }
        catch (InvalidOperationException)
        {
            if (FinallyRan != 1) return 2;
            Console.WriteLine("eh: eh_finally_on_throw_path ok");
            return 0;
        }
    }
}
