// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if (Convert.ToString(42) != "42") return 1;
        Console.WriteLine("format: convert_int_to_string ok");
        return 0;
    }
}
