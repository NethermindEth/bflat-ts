// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var outer = new List<List<int>>();
        for (int i = 0; i < 10; i++) outer.Add(new List<int> { i });
        if (outer.Count != 10) return 1;
        Console.WriteLine("pal: many_lists_10 ok");
        return 0;
    }
}
