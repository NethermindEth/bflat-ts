// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;



class Program
{
    static int Main()
    {
        var d = new Dictionary<string, int>();
        d["a"] = 10;
        if (!d.ContainsKey("a")) return 1;
        if (d.ContainsKey("b")) return 1;
        Console.WriteLine("gvm: dict_containskey_string ok");
        return 0;
    }
}
