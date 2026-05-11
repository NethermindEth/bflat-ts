// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        var r = new Random();
        int v = r.Next(1000);
        if (v < 0 || v >= 1000) return 1;
        Console.WriteLine("rng: next_upper_1000 ok");
        return 0;
    }
}
