// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        for (int i = 0; i < 5000; i++) { var o = new object(); GC.KeepAlive(o); }
        Console.WriteLine("memory: alloc_then_drop_5000 ok");
        return 0;
    }
}
