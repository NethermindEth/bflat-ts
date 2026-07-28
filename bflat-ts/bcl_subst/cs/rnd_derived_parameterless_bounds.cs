// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Parameterless derived Random (lazy EnsureInitialized in the snippet):
// values stay in range and are not constant.
using System;

class DRand : Random
{
}

class Program
{
    static int Main()
    {
        var r = new DRand();
        int first = r.Next(1000);
        bool varied = false;
        for (int i = 0; i < 200; i++)
        {
            int x = r.Next(1000);
            if (x < 0 || x >= 1000) return 1;
            if (x != first) varied = true;
        }
        if (!varied) return 2;
        Console.WriteLine("bcl_subst: rnd_derived_parameterless_bounds ok");
        return 0;
    }
}
