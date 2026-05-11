// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ((1).ToString("x") != "1") return 1;
        Console.WriteLine("format: int_hex_lower_1 ok");
        return 0;
    }
}
