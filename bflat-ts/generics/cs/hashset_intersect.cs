// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var a = new HashSet<int> { 1, 2, 3 };
        a.IntersectWith(new[] { 2, 3, 4 });
        if (a.Count != 2) return 1;
        Console.WriteLine("generics: hashset_intersect ok");
        return 0;
    }
}
