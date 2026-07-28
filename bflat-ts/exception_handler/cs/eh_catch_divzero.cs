// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Integer division by zero must raise a catchable DivideByZeroException.
// RISC-V div does not trap, so the runtime emits an explicit check.
using System;

class Program
{
    static int Denominator; // static so Roslyn cannot fold the division

    static int Main()
    {
        try
        {
            int q = 100 / Denominator;
            GC.KeepAlive(q);
            return 1;
        }
        catch (DivideByZeroException)
        {
            Console.WriteLine("eh: eh_catch_divzero ok");
            return 0;
        }
    }
}
