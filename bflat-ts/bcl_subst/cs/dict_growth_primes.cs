// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Dictionary growth marches through GetPrime -> IsPrime, which zisk diverts
// to the integer rhp reimplementation. Many resizes, full verification.
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var d = new Dictionary<int, int>();
        for (int i = 0; i < 3000; i++)
            d[i] = i * 2 + 1;
        if (d.Count != 3000) return 1;
        for (int i = 0; i < 3000; i++)
            if (d[i] != i * 2 + 1) return 2;
        if (d.ContainsKey(3000)) return 3;
        Console.WriteLine("bcl_subst: dict_growth_primes ok");
        return 0;
    }
}
