// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var kv = new KeyValuePair<int, string>(1, "x");
        if (kv.Key != 1) return 1;
        if (kv.Value != "x") return 1;
        Console.WriteLine("generics: kvp_basic ok");
        return 0;
    }
}
