// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if (0.5.ToString() != "0.5") return 1;
        Console.WriteLine("format: double_one_half ok");
        return 0;
    }
}
