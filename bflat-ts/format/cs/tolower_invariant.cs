// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ("ABC".ToLowerInvariant() != "abc") return 1;
        Console.WriteLine("format: tolower_invariant ok");
        return 0;
    }
}
