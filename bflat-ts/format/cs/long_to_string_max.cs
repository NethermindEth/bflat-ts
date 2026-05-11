// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if (long.MaxValue.ToString() != "9223372036854775807") return 1;
        Console.WriteLine("format: long_to_string_max ok");
        return 0;
    }
}
