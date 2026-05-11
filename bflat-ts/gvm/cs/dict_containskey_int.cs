// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;



class Program
{
    static int Main()
    {
        var d = new Dictionary<int, int>();
        d[1] = 10;
        if (!d.ContainsKey(1)) return 1;
        if (d.ContainsKey(2)) return 1;
        Console.WriteLine("gvm: dict_containskey_int ok");
        return 0;
    }
}
