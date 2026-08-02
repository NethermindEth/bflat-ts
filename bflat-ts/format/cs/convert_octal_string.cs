// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if (Convert.ToString(8, 8) != "10") return 1;
        Console.WriteLine("format: convert_octal_string ok");
        return 0;
    }
}
