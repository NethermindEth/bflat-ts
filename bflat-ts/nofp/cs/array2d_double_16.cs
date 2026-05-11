// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        double[][] a = new double[16][];
        for (int i = 0; i < 16; i++) a[i] = new double[16];
        a[0][0] = 1.0;
        GC.KeepAlive(a);
        Console.WriteLine("nofp: array2d_double_16 ok");
        return 0;
    }
}
