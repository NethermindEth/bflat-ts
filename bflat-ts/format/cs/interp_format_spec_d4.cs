// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        int x = 7;
        if ($"{x:D4}" != "0007") return 1;
        Console.WriteLine("format: interp_format_spec_d4 ok");
        return 0;
    }
}
