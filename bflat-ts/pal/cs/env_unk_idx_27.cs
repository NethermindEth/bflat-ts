// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        string v = Environment.GetEnvironmentVariable("UNK_27_NAME");
        if (v != null) return 1;
        Console.WriteLine("pal: env_unk_idx_27 ok");
        return 0;
    }
}
