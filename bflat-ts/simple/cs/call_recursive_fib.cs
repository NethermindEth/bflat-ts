// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Fib(int n) { return n < 2 ? n : Fib(n - 1) + Fib(n - 2); }

    static int Main()
    {
        Console.WriteLine(Fib(10));
        return 0;
    }
}
