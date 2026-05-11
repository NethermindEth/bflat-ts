// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        GC.Collect();
        var x = new object();
        GC.KeepAlive(x);
        Console.WriteLine("pal: gc_collect_simple ok");
        return 0;
    }
}
