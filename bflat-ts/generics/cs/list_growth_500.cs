// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var l = new List<int>();
        for (int i = 0; i < 500; i++) l.Add(i);
        if (l.Count != 500) return 1;
        if (l[499] != 499) return 1;
        Console.WriteLine("generics: list_growth_500 ok");
        return 0;
    }
}
