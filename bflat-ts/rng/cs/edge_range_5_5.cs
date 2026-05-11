// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        var r = new Random();
        int v = r.Next(5, 6);
        if (v < 5 || v > 5) return 1;
        Console.WriteLine("rng: edge_range_5_5 ok");
        return 0;
    }
}
