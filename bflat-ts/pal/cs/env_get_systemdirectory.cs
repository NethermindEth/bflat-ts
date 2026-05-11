// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var v = Environment.SystemDirectory;
        GC.KeepAlive(v);
        Console.WriteLine("pal: env_get_systemdirectory ok");
        return 0;
    }
}
