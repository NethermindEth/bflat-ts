// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        var r = new Random();
        int v = r.Next(0, 1);
        if (v < 0 || v > 0) return 1;
        Console.WriteLine("rng: edge_range_0_0 ok");
        return 0;
    }
}
