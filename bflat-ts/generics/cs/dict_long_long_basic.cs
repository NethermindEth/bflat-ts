// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var d = new Dictionary<long, long>();
        d[1L] = 2L;
        if (!d.ContainsKey(1L)) return 1;
        Console.WriteLine("generics: dict_long_long_basic ok");
        return 0;
    }
}
