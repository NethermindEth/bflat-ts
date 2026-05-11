// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var l = new List<double>();
        l.Add(1.0);
        l.Add(2.0);
        if (l.Count != 2) return 1;
        Console.WriteLine("generics: list_double_add ok");
        return 0;
    }
}
