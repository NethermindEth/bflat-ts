// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;



class Program
{
    static int Main()
    {
        var l = new List<int>() { 1, 2, 3 };
        if (!l.Contains(2)) return 1;
        Console.WriteLine("gvm: list_contains_int ok");
        return 0;
    }
}
