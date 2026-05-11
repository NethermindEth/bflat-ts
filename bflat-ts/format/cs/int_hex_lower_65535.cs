// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ((65535).ToString("x") != "ffff") return 1;
        Console.WriteLine("format: int_hex_lower_65535 ok");
        return 0;
    }
}
