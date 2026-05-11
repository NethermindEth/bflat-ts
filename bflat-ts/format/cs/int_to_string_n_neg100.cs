// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ((-100).ToString() != "-100") return 1;
        Console.WriteLine("format: int_to_string_n_-100 ok");
        return 0;
    }
}
