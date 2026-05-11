// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if (uint.MaxValue.ToString() != "4294967295") return 1;
        Console.WriteLine("format: uint_to_string_max ok");
        return 0;
    }
}
