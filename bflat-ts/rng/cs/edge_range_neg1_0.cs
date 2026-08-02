// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        var r = new Random();
        int v = r.Next(-1, 1);
        if (v < -1 || v > 0) return 1;
        Console.WriteLine("rng: edge_range_neg1_0 ok");
        return 0;
    }
}
