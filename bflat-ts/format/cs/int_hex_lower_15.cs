// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ((15).ToString("x") != "f") return 1;
        Console.WriteLine("format: int_hex_lower_15 ok");
        return 0;
    }
}
