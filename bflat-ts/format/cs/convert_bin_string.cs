// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if (Convert.ToString(5, 2) != "101") return 1;
        Console.WriteLine("format: convert_bin_string ok");
        return 0;
    }
}
