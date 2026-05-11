// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        var r = new Random();
        int v = r.Next(0, 2);
        if (v < 0 || v > 1) return 1;
        Console.WriteLine("rng: edge_range_0_1 ok");
        return 0;
    }
}
