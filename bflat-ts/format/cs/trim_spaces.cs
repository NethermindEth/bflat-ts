// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ("  hi  ".Trim() != "hi") return 1;
        Console.WriteLine("format: trim_spaces ok");
        return 0;
    }
}
