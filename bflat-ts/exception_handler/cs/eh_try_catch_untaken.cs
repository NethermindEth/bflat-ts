// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// A catch clause that never fires must not disturb normal control flow.
using System;

class Program
{
    static int Add(int a, int b)
    {
        try
        {
            return a + b;
        }
        catch (Exception)
        {
            return -1;
        }
    }

    static int Main()
    {
        if (Add(2, 3) != 5) return 1;
        if (Add(-7, 7) != 0) return 2;
        Console.WriteLine("eh: eh_try_catch_untaken ok");
        return 0;
    }
}
