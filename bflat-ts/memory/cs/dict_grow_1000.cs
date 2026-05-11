// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var d = new Dictionary<int, int>();
        for (int i = 0; i < 1000; i++) d[i] = i;
        if (d.Count != 1000) return 1;
        Console.WriteLine("memory: dict_grow_1000 ok");
        return 0;
    }
}
