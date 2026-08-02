// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Stopwatch raw ElapsedTicks (integer, via the wrapped clock_gettime) must
// be monotonic; only the raw-tick API is used (Elapsed scales via double).
using System;
using System.Diagnostics;

class Program
{
    static int Main()
    {
        var sw = Stopwatch.StartNew();
        long acc = 0;
        for (int i = 0; i < 100000; i++)
            acc += i;
        sw.Stop();
        if (acc != 4999950000L) return 1;
        if (sw.ElapsedTicks < 0) return 2;
        long first = sw.ElapsedTicks;
        if (sw.ElapsedTicks != first) return 3;
        Console.WriteLine("bcl_subst: stopwatch_elapsed_ticks ok");
        return 0;
    }
}
