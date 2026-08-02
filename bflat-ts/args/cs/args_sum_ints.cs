// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main(string[] args)
    {
        int s = 0;
        for (int i = 0; i < args.Length; i++)
            if (int.TryParse(args[i], out int v)) s += v;
        Console.WriteLine(s);
        return 0;
    }
}
