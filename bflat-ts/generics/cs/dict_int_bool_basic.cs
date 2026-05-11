// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var d = new Dictionary<int, bool>();
        d[1] = true;
        if (!d.ContainsKey(1)) return 1;
        Console.WriteLine("generics: dict_int_bool_basic ok");
        return 0;
    }
}
