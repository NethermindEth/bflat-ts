// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        var r = new Random(-1);
        int v = r.Next(0, 1000);
        if (v < 0 || v >= 1000) return 1;
        Console.WriteLine("rng: seeded_neg1 ok");
        return 0;
    }
}
