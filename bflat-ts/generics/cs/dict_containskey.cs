// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var d = new Dictionary<int, string>();
        d[1] = "one";
        if (!d.ContainsKey(1)) return 1;
        if (d.ContainsKey(2)) return 1;
        Console.WriteLine("generics: dict_containskey ok");
        return 0;
    }
}
