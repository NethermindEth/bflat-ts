// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        var parts = new string[] { "a", "b", "c" };
        if (string.Join(",", parts) != "a,b,c") return 1;
        Console.WriteLine("format: join_comma ok");
        return 0;
    }
}
