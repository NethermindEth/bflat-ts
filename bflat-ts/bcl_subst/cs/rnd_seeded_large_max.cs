// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Seeded Random Next(int.MaxValue): the (long)sample * range / int.MaxValue
// scaling must not overflow or leave [0, max).
using System;

class Program
{
    static int Main()
    {
        var r = new Random(31337);
        for (int i = 0; i < 2000; i++)
        {
            int x = r.Next(int.MaxValue);
            if (x < 0) return 1;
        }
        Console.WriteLine("bcl_subst: rnd_seeded_large_max ok");
        return 0;
    }
}
