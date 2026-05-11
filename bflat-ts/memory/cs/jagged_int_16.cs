// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        int[][] j = new int[16][];
        for (int i = 0; i < 16; i++) j[i] = new int[16];
        j[0][0] = 7; if (j[0][0] != 7) return 1;
        Console.WriteLine("memory: jagged_int_16 ok");
        return 0;
    }
}
