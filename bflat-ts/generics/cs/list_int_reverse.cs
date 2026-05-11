// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var l = new List<int> { 1, 2, 3 };
        l.Reverse();
        if (l[0] != 3) return 1;
        Console.WriteLine("generics: list_int_reverse ok");
        return 0;
    }
}
