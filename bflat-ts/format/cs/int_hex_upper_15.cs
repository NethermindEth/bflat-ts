// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ((15).ToString("X") != "F") return 1;
        Console.WriteLine("format: int_hex_upper_15 ok");
        return 0;
    }
}
