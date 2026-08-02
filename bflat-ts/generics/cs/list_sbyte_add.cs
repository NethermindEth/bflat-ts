// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var l = new List<sbyte>();
        l.Add((sbyte)1);
        l.Add((sbyte)2);
        if (l.Count != 2) return 1;
        Console.WriteLine("generics: list_sbyte_add ok");
        return 0;
    }
}
