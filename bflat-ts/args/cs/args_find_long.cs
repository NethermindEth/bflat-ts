// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main(string[] args)
    {
        string longest = "";
        foreach (var a in args) if (a.Length > longest.Length) longest = a;
        Console.WriteLine(longest);
        return 0;
    }
}
