// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ((-1).ToString() != "-1") return 1;
        Console.WriteLine("format: int_to_string_neg_one ok");
        return 0;
    }
}
