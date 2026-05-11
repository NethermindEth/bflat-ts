// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if (int.MaxValue.ToString() != "2147483647") return 1;
        Console.WriteLine("format: int_to_string_max ok");
        return 0;
    }
}
