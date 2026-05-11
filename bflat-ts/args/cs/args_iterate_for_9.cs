// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main(string[] args)
    {
        for (int j = 0; j < args.Length && j < 9; j++)
            Console.WriteLine(args[j]);
        return 0;
    }
}
