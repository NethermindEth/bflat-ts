// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var l = new List<int>();
        l.Add(1); l.Add(2);
        if (!l.Contains(1)) return 1;
        if (l.Contains(99)) return 1;
        Console.WriteLine("generics: list_int_contains ok");
        return 0;
    }
}
