// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        var r = new Random();
        int v = r.Next(10000);
        if (v < 0 || v >= 10000) return 1;
        Console.WriteLine("rng: next_upper_10000 ok");
        return 0;
    }
}
