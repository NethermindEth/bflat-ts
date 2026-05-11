// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var d = new Dictionary<int, int>();
        d.Add(1, 10); d.Add(2, 20); d.Add(3, 30);
        if (d.Count != 3) return 1;
        Console.WriteLine("generics: dict_count ok");
        return 0;
    }
}
