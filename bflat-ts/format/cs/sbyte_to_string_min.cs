// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if (sbyte.MinValue.ToString() != "-128") return 1;
        Console.WriteLine("format: sbyte_to_string_min ok");
        return 0;
    }
}
