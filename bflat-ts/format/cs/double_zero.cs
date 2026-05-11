// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if (0.0.ToString() != "0") return 1;
        Console.WriteLine("format: double_zero ok");
        return 0;
    }
}
