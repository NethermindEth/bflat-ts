// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ($"{2 + 3}" != "5") return 1;
        Console.WriteLine("format: interp_int_plus ok");
        return 0;
    }
}
