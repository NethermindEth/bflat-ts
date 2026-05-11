// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var l = new List<ulong>();
        l.Add(1UL);
        l.Add(2UL);
        if (l.Count != 2) return 1;
        Console.WriteLine("generics: list_ulong_add ok");
        return 0;
    }
}
