// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Seeded Random Next(min, max) over a range wider than int.MaxValue: the
// range fits only in long, exercising the 64-bit scaling in the snippet.
using System;

class Program
{
    static int Main()
    {
        var r = new Random(4242);
        for (int i = 0; i < 2000; i++)
        {
            int x = r.Next(int.MinValue + 1, int.MaxValue);
            if (x == int.MaxValue) return 1;
        }
        var s = new Random(4242);
        var t = new Random(4242);
        for (int i = 0; i < 100; i++)
            if (s.Next(int.MinValue + 1, int.MaxValue) !=
                t.Next(int.MinValue + 1, int.MaxValue)) return 2;
        Console.WriteLine("bcl_subst: rnd_seeded_wide_range ok");
        return 0;
    }
}
