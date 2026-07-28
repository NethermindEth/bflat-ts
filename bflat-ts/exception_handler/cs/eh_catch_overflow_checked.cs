// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// checked arithmetic overflow must raise a catchable OverflowException.
using System;

class Program
{
    static int Big = int.MaxValue; // static so the overflow cannot be folded

    static int Main()
    {
        try
        {
            int r = checked(Big + 1);
            GC.KeepAlive(r);
            return 1;
        }
        catch (OverflowException)
        {
            Console.WriteLine("eh: eh_catch_overflow_checked ok");
            return 0;
        }
    }
}
