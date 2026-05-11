// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var d = new Dictionary<string, List<int>>();
        d["a"] = new List<int> { 1, 2 };
        if (d["a"].Count != 2) return 1;
        Console.WriteLine("generics: nested_dict_of_list ok");
        return 0;
    }
}
