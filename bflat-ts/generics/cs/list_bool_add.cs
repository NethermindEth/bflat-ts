// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var l = new List<bool>();
        l.Add(true);
        l.Add(false);
        if (l.Count != 2) return 1;
        Console.WriteLine("generics: list_bool_add ok");
        return 0;
    }
}
