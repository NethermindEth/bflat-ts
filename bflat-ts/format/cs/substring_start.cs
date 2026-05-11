// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ("hello".Substring(0, 1) != "h") return 1;
        Console.WriteLine("format: substring_start ok");
        return 0;
    }
}
