// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        int[,] g = new int[2, 2];
        g[0, 0] = 1; g[1, 1] = 2;
        if (g[0, 0] + g[1, 1] != 3) return 1;
        Console.WriteLine("rhp: multidim_int_2 ok");
        return 0;
    }
}
