// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var s = new HashSet<int>();
        s.Add(1); s.Add(2); s.Add(1);
        if (s.Count != 2) return 1;
        if (!s.Contains(1)) return 1;
        Console.WriteLine("generics: hashset_int_basic ok");
        return 0;
    }
}
