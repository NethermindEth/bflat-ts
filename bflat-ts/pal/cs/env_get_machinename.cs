// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var v = Environment.MachineName;
        GC.KeepAlive(v);
        Console.WriteLine("pal: env_get_machinename ok");
        return 0;
    }
}
