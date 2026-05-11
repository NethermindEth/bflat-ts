// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        string v = Environment.GetEnvironmentVariable("SOMETHING_ELSE");
        if (v != null) return 1;
        Console.WriteLine("pal: env_unknown_something_else ok");
        return 0;
    }
}
