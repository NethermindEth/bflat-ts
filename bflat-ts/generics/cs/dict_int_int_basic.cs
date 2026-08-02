// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var d = new Dictionary<int, int>();
        d[1] = 10;
        d[2] = 20;
        if (d[1] != 10) return 1;
        if (d[2] != 20) return 1;
        Console.WriteLine("generics: dict_int_int_basic ok");
        return 0;
    }
}
