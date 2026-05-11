// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        var r = new Random();
        int v = r.Next(1, 1000000);
        if (v < 1 || v >= 1000000) return 1;
        Console.WriteLine("rng: next_range_1_1000000 ok");
        return 0;
    }
}
