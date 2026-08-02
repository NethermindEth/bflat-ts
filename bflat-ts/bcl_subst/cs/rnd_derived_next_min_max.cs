// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Derived Random Next(min, max) integer-scaling path: bounds with negative
// minimum, determinism, and the empty range min == max.
using System;

class DRand : Random
{
    public DRand(int seed) : base(seed) { }
}

class Program
{
    static int Main()
    {
        var a = new DRand(777);
        var b = new DRand(777);
        for (int i = 0; i < 1000; i++)
        {
            int x = a.Next(-50, 50);
            if (x != b.Next(-50, 50)) return 1;
            if (x < -50 || x >= 50) return 2;
        }
        if (a.Next(7, 7) != 7) return 3;
        Console.WriteLine("bcl_subst: rnd_derived_next_min_max ok");
        return 0;
    }
}
