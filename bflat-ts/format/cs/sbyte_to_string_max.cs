// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if (sbyte.MaxValue.ToString() != "127") return 1;
        Console.WriteLine("format: sbyte_to_string_max ok");
        return 0;
    }
}
