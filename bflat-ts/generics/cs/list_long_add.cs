// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var l = new List<long>();
        l.Add(1L);
        l.Add(2L);
        if (l.Count != 2) return 1;
        Console.WriteLine("generics: list_long_add ok");
        return 0;
    }
}
