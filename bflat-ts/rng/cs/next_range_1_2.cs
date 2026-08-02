// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        var r = new Random();
        int v = r.Next(1, 2);
        if (v < 1 || v >= 2) return 1;
        Console.WriteLine("rng: next_range_1_2 ok");
        return 0;
    }
}
