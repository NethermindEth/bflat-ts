// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ("  hi".TrimStart() != "hi") return 1;
        Console.WriteLine("format: trimstart_spaces ok");
        return 0;
    }
}
