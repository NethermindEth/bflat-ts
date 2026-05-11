// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var l = new List<int>(1024);
        for (int i = 0; i < 1024; i++) l.Add(i);
        if (l.Count != 1024) return 1;
        Console.WriteLine("generics: list_capacity_1024 ok");
        return 0;
    }
}
