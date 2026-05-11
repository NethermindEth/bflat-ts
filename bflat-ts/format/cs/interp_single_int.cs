// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ($"{42}" != "42") return 1;
        Console.WriteLine("format: interp_single_int ok");
        return 0;
    }
}
