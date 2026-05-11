// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main(string[] args)
    {
        Array.Reverse(args);
        Console.WriteLine(args.Length);
        return 0;
    }
}
