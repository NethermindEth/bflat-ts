// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var d = new Dictionary<int, int>();
        d[1] = 10;
        if (!d.TryGetValue(1, out int v) || v != 10) return 1;
        if (d.TryGetValue(2, out int _)) return 1;
        Console.WriteLine("generics: dict_trygetvalue ok");
        return 0;
    }
}
