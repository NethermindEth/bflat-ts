// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var l = new List<int>();
        for (int i = 0; i < 100; i++) l.Add(i);
        if (l.Count != 100) return 1;
        Console.WriteLine("memory: list_grow_100 ok");
        return 0;
    }
}
