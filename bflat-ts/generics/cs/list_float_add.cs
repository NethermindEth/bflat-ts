// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var l = new List<float>();
        l.Add(1f);
        l.Add(2f);
        if (l.Count != 2) return 1;
        Console.WriteLine("generics: list_float_add ok");
        return 0;
    }
}
