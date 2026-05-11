// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        if ("hello".Replace('l', 'L') != "heLLo") return 1;
        Console.WriteLine("format: replace_char ok");
        return 0;
    }
}
