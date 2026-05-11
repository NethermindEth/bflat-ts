// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main(string[] args)
    {
        string s = args.Length > 0 ? "first=" + args[0] : "none";
        Console.WriteLine(s);
        return 0;
    }
}
