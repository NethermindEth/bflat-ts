// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var l = new List<int>();
        l.Add(1); l.Add(2); l.Add(3);
        l.Remove(2);
        if (l.Count != 2) return 1;
        if (l[0] != 1 || l[1] != 3) return 1;
        Console.WriteLine("generics: list_int_remove ok");
        return 0;
    }
}
