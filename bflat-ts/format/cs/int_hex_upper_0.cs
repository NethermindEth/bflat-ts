// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ((0).ToString("X") != "0") return 1;
        Console.WriteLine("format: int_hex_upper_0 ok");
        return 0;
    }
}
