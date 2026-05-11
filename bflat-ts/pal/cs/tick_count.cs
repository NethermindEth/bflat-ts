// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        int t = Environment.TickCount;
        GC.KeepAlive(t);
        Console.WriteLine("pal: tick_count ok");
        return 0;
    }
}
