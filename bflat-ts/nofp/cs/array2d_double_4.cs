// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        double[][] a = new double[4][];
        for (int i = 0; i < 4; i++) a[i] = new double[4];
        a[0][0] = 1.0;
        GC.KeepAlive(a);
        Console.WriteLine("nofp: array2d_double_4 ok");
        return 0;
    }
}
