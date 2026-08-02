// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var list = new List<int>();
        list.Add(5); list.Add(3); list.Add(1);
        if (list.Count != 3) return 1;
        list.Sort();
        if (list[0] != 1) return 1;
        Console.WriteLine("generics: list_int ok");
        return 0;
    }
}
