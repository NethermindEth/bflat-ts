// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main(string[] args)
    {
        var copy = new string[args.Length];
        Array.Copy(args, copy, args.Length);
        Console.WriteLine(copy.Length);
        return 0;
    }
}
