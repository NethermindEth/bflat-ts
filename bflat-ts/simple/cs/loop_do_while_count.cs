// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        int i = 0, sum = 0;
        do { sum += i; i++; } while (i < 5);
        Console.WriteLine(sum);
        return 0;
    }
}
