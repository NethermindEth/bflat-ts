// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var v = Environment.OSVersion;
        GC.KeepAlive(v);
        Console.WriteLine("pal: os_version ok");
        return 0;
    }
}
