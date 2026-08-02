// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        var r = new Random();
        long s = 0; for (int i = 0; i < 100; i++) s += r.Next(0, 100);
        if (s < 0) return 1;
        Console.WriteLine("rng: many_next_100 ok");
        return 0;
    }
}
