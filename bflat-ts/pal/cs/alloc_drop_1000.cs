// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        for (int i = 0; i < 1000; i++) { var x = new object(); GC.KeepAlive(x); }
        Console.WriteLine("pal: alloc_drop_1000 ok");
        return 0;
    }
}
