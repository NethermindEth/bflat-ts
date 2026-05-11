// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        int[,] g = new int[4, 4];
        g[0, 0] = 1; g[3, 3] = 2;
        if (g[0, 0] + g[3, 3] != 3) return 1;
        Console.WriteLine("rhp: multidim_int_4 ok");
        return 0;
    }
}
