// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static void Sum(params int[] xs) { int s = 0; foreach (var x in xs) s += x; Console.WriteLine(s); }

    static int Main(string[] args)
    {
        Sum(args.Length);
        return 0;
    }
}
