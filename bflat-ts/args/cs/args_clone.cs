// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main(string[] args)
    {
        var c = (string[])args.Clone();
        Console.WriteLine(c.Length);
        return 0;
    }
}
