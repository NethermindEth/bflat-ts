// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var l = new List<int>();
        l.Add(1); l.Add(2); l.Add(3);
        l.RemoveAt(0);
        if (l[0] != 2) return 1;
        Console.WriteLine("generics: list_int_removeat ok");
        return 0;
    }
}
