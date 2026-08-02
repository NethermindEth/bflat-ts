// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if (short.MaxValue.ToString() != "32767") return 1;
        Console.WriteLine("format: short_to_string_max ok");
        return 0;
    }
}
