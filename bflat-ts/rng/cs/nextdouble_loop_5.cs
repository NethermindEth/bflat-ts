// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        var r = new Random();
        for (int i = 0; i < 5; i++) {
            double v = r.NextDouble();
            if (v < 0.0 || v >= 1.0) return 1;
        }
        Console.WriteLine("rng: nextdouble_loop_5 ok");
        return 0;
    }
}
