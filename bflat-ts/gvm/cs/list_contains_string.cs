// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;



class Program
{
    static int Main()
    {
        var l = new List<string>() { "a", "b" };
        if (!l.Contains("a")) return 1;
        Console.WriteLine("gvm: list_contains_string ok");
        return 0;
    }
}
