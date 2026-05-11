// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var d = new Dictionary<int, string>();
        d[1] = "x";
        if (!d.ContainsKey(1)) return 1;
        Console.WriteLine("generics: dict_int_string_basic ok");
        return 0;
    }
}
