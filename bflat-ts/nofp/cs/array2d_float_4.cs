// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        float[][] a = new float[4][];
        for (int i = 0; i < 4; i++) a[i] = new float[4];
        a[0][0] = 1f;
        GC.KeepAlive(a);
        Console.WriteLine("nofp: array2d_float_4 ok");
        return 0;
    }
}
