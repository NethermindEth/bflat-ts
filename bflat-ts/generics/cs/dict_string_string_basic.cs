// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var d = new Dictionary<string, string>();
        d["a"] = "x";
        if (!d.ContainsKey("a")) return 1;
        Console.WriteLine("generics: dict_string_string_basic ok");
        return 0;
    }
}
