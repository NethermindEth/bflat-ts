// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ((16).ToString("X") != "10") return 1;
        Console.WriteLine("format: int_hex_upper_16 ok");
        return 0;
    }
}
