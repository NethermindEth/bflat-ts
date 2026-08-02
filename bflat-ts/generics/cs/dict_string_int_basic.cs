// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var d = new Dictionary<string, int>();
        d["a"] = 1;
        if (!d.ContainsKey("a")) return 1;
        Console.WriteLine("generics: dict_string_int_basic ok");
        return 0;
    }
}
