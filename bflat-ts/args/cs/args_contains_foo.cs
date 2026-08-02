// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main(string[] args)
    {
        bool has = Array.IndexOf(args, "foo") >= 0;
        Console.WriteLine(has);
        return 0;
    }
}
