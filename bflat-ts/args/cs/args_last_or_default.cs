// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main(string[] args)
    {
        string last = args.Length == 0 ? "none" : args[args.Length - 1];
        Console.WriteLine(last);
        return 0;
    }
}
