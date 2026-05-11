// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;



class Program
{
    static int Main()
    {
        var l = new List<long>() { 1L, 2L, 3L };
        if (!l.Contains(3L)) return 1;
        Console.WriteLine("gvm: list_contains_long ok");
        return 0;
    }
}
