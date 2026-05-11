// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        var parts = new string[] { "x", "y" };
        if (string.Join("|", parts) != "x|y") return 1;
        Console.WriteLine("format: join_pipe ok");
        return 0;
    }
}
