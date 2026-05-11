// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        string v = Environment.GetEnvironmentVariable("RANDOM_UNDEFINED_VAR");
        if (v != null) return 1;
        Console.WriteLine("pal: env_unknown_random_undefined_var ok");
        return 0;
    }
}
