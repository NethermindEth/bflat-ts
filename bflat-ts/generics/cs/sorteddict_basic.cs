// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var d = new SortedDictionary<int, int>();
        d[3] = 30; d[1] = 10; d[2] = 20;
        int prev = -1;
        foreach (var kv in d) { if (kv.Key < prev) return 1; prev = kv.Key; }
        Console.WriteLine("generics: sorteddict_basic ok");
        return 0;
    }
}
