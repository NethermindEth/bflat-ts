// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// ConcurrentDictionary growth goes through the Concurrent assembly's own
// HashHelpers copy, also diverted to the integer IsPrime; its lock striping
// is sized by the stubbed ProcessorCount=1.
using System;
using System.Collections.Concurrent;

class Program
{
    static int Main()
    {
        var d = new ConcurrentDictionary<int, int>();
        for (int i = 0; i < 2000; i++)
            if (!d.TryAdd(i, i * 5)) return 1;
        if (d.Count != 2000) return 2;
        for (int i = 0; i < 2000; i++)
            if (d[i] != i * 5) return 3;
        if (!d.TryUpdate(7, -7, 35)) return 4;
        if (d[7] != -7) return 5;
        if (!d.TryRemove(8, out int gone) || gone != 40) return 6;
        if (d.ContainsKey(8)) return 7;
        Console.WriteLine("bcl_subst: cdict_growth_primes ok");
        return 0;
    }
}
