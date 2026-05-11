// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var s = new HashSet<int>();
        s.Add(1); s.Add(2);
        s.Remove(1);
        if (s.Count != 1) return 1;
        Console.WriteLine("generics: hashset_remove ok");
        return 0;
    }
}
