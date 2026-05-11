// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if (false.ToString() != "False") return 1;
        Console.WriteLine("format: bool_false_to_string ok");
        return 0;
    }
}
