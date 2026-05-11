// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        var r = new Random(449);
        int v = r.Next(0, 100);
        if (v < 0 || v >= 100) return 1;
        Console.WriteLine("rng: seed_idx_34 ok");
        return 0;
    }
}
