// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var l = new List<string>();
        l.Add("a");
        l.Add("b");
        if (l.Count != 2) return 1;
        Console.WriteLine("generics: list_string_add ok");
        return 0;
    }
}
