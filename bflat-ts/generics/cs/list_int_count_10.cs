// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var l = new List<int>();
        for (int i = 0; i < 10; i++) l.Add(i);
        if (l.Count != 10) return 1;
        Console.WriteLine("generics: list_int_count_10 ok");
        return 0;
    }
}
