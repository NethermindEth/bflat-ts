// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var d = new Dictionary<int, int>();
        d[1] = 10; d[2] = 20;
        d.Remove(1);
        if (d.Count != 1) return 1;
        if (d.ContainsKey(1)) return 1;
        Console.WriteLine("generics: dict_remove ok");
        return 0;
    }
}
