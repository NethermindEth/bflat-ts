// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var l = new List<int>();
        for (int i = 0; i < 2; i++) l.Add(i);
        if (l.Count != 2) return 1;
        Console.WriteLine("generics: list_int_count_2 ok");
        return 0;
    }
}
