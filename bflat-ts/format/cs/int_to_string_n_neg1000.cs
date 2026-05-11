// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ((-1000).ToString() != "-1000") return 1;
        Console.WriteLine("format: int_to_string_n_-1000 ok");
        return 0;
    }
}
