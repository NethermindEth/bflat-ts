// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var l = new List<int> { 3, 1, 2 };
        l.Sort();
        if (l[0] != 1 || l[1] != 2 || l[2] != 3) return 1;
        Console.WriteLine("generics: list_int_sort ok");
        return 0;
    }
}
