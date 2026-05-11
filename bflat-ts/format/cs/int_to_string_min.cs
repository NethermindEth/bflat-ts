// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if (int.MinValue.ToString() != "-2147483648") return 1;
        Console.WriteLine("format: int_to_string_min ok");
        return 0;
    }
}
