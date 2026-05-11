// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;



class Program
{
    static int Main()
    {
        var l = new List<int>();
        l.Add(1);
        if (l.Count != 1) return 1;
        Console.WriteLine("gvm: generic_list_init_int ok");
        return 0;
    }
}
