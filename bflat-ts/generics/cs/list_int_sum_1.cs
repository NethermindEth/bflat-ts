// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var l = new List<int>();
        for (int i = 1; i <= 1; i++) l.Add(i);
        int s = 0; foreach (var x in l) s += x;
        if (s != 1) return 1;
        Console.WriteLine("generics: list_int_sum_1 ok");
        return 0;
    }
}
