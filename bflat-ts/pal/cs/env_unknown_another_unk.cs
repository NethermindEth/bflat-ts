// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        string v = Environment.GetEnvironmentVariable("ANOTHER_UNK");
        if (v != null) return 1;
        Console.WriteLine("pal: env_unknown_another_unk ok");
        return 0;
    }
}
