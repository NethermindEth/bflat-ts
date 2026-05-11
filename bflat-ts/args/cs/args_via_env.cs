// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        var e = Environment.GetCommandLineArgs();
        Console.WriteLine(e.Length);
        return 0;
    }
}
