// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ("hello".Substring(2) != "llo") return 1;
        Console.WriteLine("format: substring_to_end ok");
        return 0;
    }
}
