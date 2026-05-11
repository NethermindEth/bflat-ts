// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        int a = 1, b = 2, c = 3;
        if ($"{a}.{b}.{c}" != "1.2.3") return 1;
        Console.WriteLine("format: interp_three_vars ok");
        return 0;
    }
}
