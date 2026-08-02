// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        string v = Environment.GetEnvironmentVariable("UNK_24_NAME");
        if (v != null) return 1;
        Console.WriteLine("pal: env_unk_idx_24 ok");
        return 0;
    }
}
