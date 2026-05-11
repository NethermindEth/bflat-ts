// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if (0u.ToString() != "0") return 1;
        Console.WriteLine("format: uint_to_string_zero ok");
        return 0;
    }
}
