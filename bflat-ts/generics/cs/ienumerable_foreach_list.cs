// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        IEnumerable<int> e = new List<int> { 1, 2, 3 };
        int s = 0; foreach (var x in e) s += x;
        if (s != 6) return 1;
        Console.WriteLine("generics: ienumerable_foreach_list ok");
        return 0;
    }
}
