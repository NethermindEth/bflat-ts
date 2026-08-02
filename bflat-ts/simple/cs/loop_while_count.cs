// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        int i = 0, sum = 0;
        while (i < 5) { sum += i; i++; }
        Console.WriteLine(sum);
        return 0;
    }
}
