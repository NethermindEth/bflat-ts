// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        int[,] g = new int[16, 16];
        g[0, 0] = 1; g[15, 15] = 2;
        if (g[0, 0] + g[15, 15] != 3) return 1;
        Console.WriteLine("memory: 2d_int_16 ok");
        return 0;
    }
}
