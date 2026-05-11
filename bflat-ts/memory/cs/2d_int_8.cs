// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        int[,] g = new int[8, 8];
        g[0, 0] = 1; g[7, 7] = 2;
        if (g[0, 0] + g[7, 7] != 3) return 1;
        Console.WriteLine("memory: 2d_int_8 ok");
        return 0;
    }
}
