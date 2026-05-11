// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var l = new List<int>();
        for (int i = 0; i < 2000; i++) l.Add(i);
        if (l.Count != 2000) return 1;
        if (l[1999] != 1999) return 1;
        Console.WriteLine("generics: list_growth_2000 ok");
        return 0;
    }
}
