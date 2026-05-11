// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        int a = 1, b = 2;
        if ($"{a}+{b}" != "1+2") return 1;
        Console.WriteLine("format: interp_two_vars ok");
        return 0;
    }
}
