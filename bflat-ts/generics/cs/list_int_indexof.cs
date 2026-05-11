// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var l = new List<int>();
        l.Add(10); l.Add(20); l.Add(30);
        if (l.IndexOf(20) != 1) return 1;
        if (l.IndexOf(99) != -1) return 1;
        Console.WriteLine("generics: list_int_indexof ok");
        return 0;
    }
}
