// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ((256).ToString("x") != "100") return 1;
        Console.WriteLine("format: int_hex_lower_256 ok");
        return 0;
    }
}
