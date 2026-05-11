// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        var r = new Random();
        int a = r.Next(0, 1000);
        int b = r.Next(0, 1000);
        int c = r.Next(0, 1000);
        if (a < 0 || a >= 1000) return 1;
        if (b < 0 || b >= 1000) return 1;
        if (c < 0 || c >= 1000) return 1;
        Console.WriteLine("rng: random_next ok");
        return 0;
    }
}
