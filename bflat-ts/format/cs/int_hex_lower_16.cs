// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ((16).ToString("x") != "10") return 1;
        Console.WriteLine("format: int_hex_lower_16 ok");
        return 0;
    }
}
