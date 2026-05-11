// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;



class Program
{
    static int Main()
    {
        var d = new Dictionary<long, int>();
        d[1L] = 10;
        if (!d.ContainsKey(1L)) return 1;
        if (d.ContainsKey(2L)) return 1;
        Console.WriteLine("gvm: dict_containskey_long ok");
        return 0;
    }
}
