// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ((255).ToString("x") != "ff") return 1;
        Console.WriteLine("format: int_hex_lower_255 ok");
        return 0;
    }
}
