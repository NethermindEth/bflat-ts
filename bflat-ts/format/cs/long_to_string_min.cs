// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if (long.MinValue.ToString() != "-9223372036854775808") return 1;
        Console.WriteLine("format: long_to_string_min ok");
        return 0;
    }
}
