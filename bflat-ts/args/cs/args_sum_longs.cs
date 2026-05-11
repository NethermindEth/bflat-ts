// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main(string[] args)
    {
        long s = 0;
        for (int i = 0; i < args.Length; i++)
            if (long.TryParse(args[i], out long v)) s += v;
        Console.WriteLine(s);
        return 0;
    }
}
