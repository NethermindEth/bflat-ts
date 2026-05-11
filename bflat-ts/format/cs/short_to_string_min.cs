// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if (short.MinValue.ToString() != "-32768") return 1;
        Console.WriteLine("format: short_to_string_min ok");
        return 0;
    }
}
