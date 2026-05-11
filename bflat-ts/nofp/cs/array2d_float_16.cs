// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static int Main()
    {
        float[][] a = new float[16][];
        for (int i = 0; i < 16; i++) a[i] = new float[16];
        a[0][0] = 1f;
        GC.KeepAlive(a);
        Console.WriteLine("nofp: array2d_float_16 ok");
        return 0;
    }
}
