// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main(string[] args)
    {
        var a = (string[])args.Clone();
        var b = (string[])args.Clone();
        Console.WriteLine(a.Length + b.Length);
        return 0;
    }
}
