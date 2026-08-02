// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// HashSet growth exercises the same wrapped prime machinery.
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var s = new HashSet<int>();
        for (int i = 0; i < 3000; i++)
            if (!s.Add(i * 7)) return 1;
        if (s.Count != 3000) return 2;
        for (int i = 0; i < 3000; i++)
            if (!s.Contains(i * 7)) return 3;
        if (s.Contains(1)) return 4;
        Console.WriteLine("bcl_subst: hashset_growth_primes ok");
        return 0;
    }
}
