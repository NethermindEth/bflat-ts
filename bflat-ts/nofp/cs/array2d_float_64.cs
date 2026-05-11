// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        float[][] a = new float[64][];
        for (int i = 0; i < 64; i++) a[i] = new float[64];
        a[0][0] = 1f;
        GC.KeepAlive(a);
        Console.WriteLine("nofp: array2d_float_64 ok");
        return 0;
    }
}
