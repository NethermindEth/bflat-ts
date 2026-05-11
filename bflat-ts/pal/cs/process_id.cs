// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        int p = Environment.ProcessId;
        GC.KeepAlive(p);
        Console.WriteLine("pal: process_id ok");
        return 0;
    }
}
