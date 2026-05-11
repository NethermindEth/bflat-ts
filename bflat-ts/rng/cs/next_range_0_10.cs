// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        var r = new Random();
        int v = r.Next(0, 10);
        if (v < 0 || v >= 10) return 1;
        Console.WriteLine("rng: next_range_0_10 ok");
        return 0;
    }
}
