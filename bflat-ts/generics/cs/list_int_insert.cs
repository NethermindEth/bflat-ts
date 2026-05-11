// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var l = new List<int>();
        l.Add(1); l.Add(3);
        l.Insert(1, 2);
        if (l[1] != 2) return 1;
        Console.WriteLine("generics: list_int_insert ok");
        return 0;
    }
}
