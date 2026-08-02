// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ("abc".ToUpperInvariant() != "ABC") return 1;
        Console.WriteLine("format: toupper_invariant ok");
        return 0;
    }
}
