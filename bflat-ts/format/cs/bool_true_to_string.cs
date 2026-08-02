// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if (true.ToString() != "True") return 1;
        Console.WriteLine("format: bool_true_to_string ok");
        return 0;
    }
}
