// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        var r = new Random();
        int v = r.Next(100);
        if (v < 0 || v >= 100) return 1;
        Console.WriteLine("rng: next_upper_100 ok");
        return 0;
    }
}
