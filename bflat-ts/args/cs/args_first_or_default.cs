// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main(string[] args)
    {
        string first = args.Length == 0 ? "none" : args[0];
        Console.WriteLine(first);
        return 0;
    }
}
