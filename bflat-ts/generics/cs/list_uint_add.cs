// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var l = new List<uint>();
        l.Add(1u);
        l.Add(2u);
        if (l.Count != 2) return 1;
        Console.WriteLine("generics: list_uint_add ok");
        return 0;
    }
}
