// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var d = new Dictionary<string, int>();
        d["a"] = 1; d["b"] = 2;
        if (d["a"] != 1 || d["b"] != 2) return 1;
        Console.WriteLine("generics: dict_basic ok");
        return 0;
    }
}
