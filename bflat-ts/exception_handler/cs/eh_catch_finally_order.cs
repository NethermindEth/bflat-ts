// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// On the throw path the order is: try prefix, catch, finally.
using System;

class Program
{
    static int Main()
    {
        int step = 0;
        try
        {
            step = 1;
            throw new Exception("x");
        }
        catch (Exception)
        {
            if (step != 1) return 1;
            step = 2;
        }
        finally
        {
            if (step == 2) step = 3;
        }
        if (step != 3) return 2;
        Console.WriteLine("eh: eh_catch_finally_order ok");
        return 0;
    }
}
