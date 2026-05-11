// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        int n = 0;
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                n++;
        Console.WriteLine(n);
        return 0;
    }
}
