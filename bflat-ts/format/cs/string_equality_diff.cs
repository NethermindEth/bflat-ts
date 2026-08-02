// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ("a".Equals("b").ToString() != "False") return 1;
        Console.WriteLine("format: string_equality_diff ok");
        return 0;
    }
}
