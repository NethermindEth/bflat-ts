// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Derived Random uses CompatDerivedImpl; its Next(max) is replaced by an
// integer-scaling snippet on zisk. Bounds + determinism for equal seeds.
using System;

class DRand : Random
{
    public DRand(int seed) : base(seed) { }
}

class Program
{
    static int Main()
    {
        var a = new DRand(12345);
        var b = new DRand(12345);
        for (int i = 0; i < 1000; i++)
        {
            int x = a.Next(100);
            int y = b.Next(100);
            if (x != y) return 1;
            if (x < 0 || x >= 100) return 2;
        }
        if (a.Next(1) != 0) return 3;
        Console.WriteLine("bcl_subst: rnd_derived_next_max ok");
        return 0;
    }
}
