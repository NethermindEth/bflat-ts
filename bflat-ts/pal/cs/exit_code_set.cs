// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        Environment.ExitCode = 0;
        int v = Environment.ExitCode;
        if (v != 0) return 1;
        Console.WriteLine("pal: exit_code_set ok");
        return 0;
    }
}
