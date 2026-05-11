// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        int sum = 0;
        for (int i = 0; i < 100; i++) { if (i == 3) break; sum += i; }
        Console.WriteLine(sum);
        return 0;
    }
}
