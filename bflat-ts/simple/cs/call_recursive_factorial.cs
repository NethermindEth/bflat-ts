// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Fact(int n) { return n <= 1 ? 1 : n * Fact(n - 1); }

    static int Main()
    {
        Console.WriteLine(Fact(5));
        return 0;
    }
}
