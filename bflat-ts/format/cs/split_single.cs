// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        var parts = "a,b,c".Split(',');
        if (parts.Length != 3) return 1;
        if (parts[0] != "a") return 1;
        if (parts[1] != "b") return 1;
        if (parts[2] != "c") return 1;
        Console.WriteLine("format: split_single ok");
        return 0;
    }
}
