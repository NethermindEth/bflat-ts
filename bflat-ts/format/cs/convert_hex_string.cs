// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if (Convert.ToString(255, 16) != "ff") return 1;
        Console.WriteLine("format: convert_hex_string ok");
        return 0;
    }
}
