// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var d = new Dictionary<string, Dictionary<int, int>>();
        d["a"] = new Dictionary<int, int> { { 1, 10 } };
        if (d["a"][1] != 10) return 1;
        Console.WriteLine("generics: nested_dict_of_dict ok");
        return 0;
    }
}
