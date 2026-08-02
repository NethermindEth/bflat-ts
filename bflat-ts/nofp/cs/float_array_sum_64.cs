// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static float[] s_a = new float[64];
    static int Main()
    {
        for (int i = 0; i < 64; i++) s_a[i] = i;
        float sum = 0;
        for (int i = 0; i < 64; i++) sum += s_a[i];
        GC.KeepAlive(sum);
        Console.WriteLine("nofp: float_array_sum_64 ok");
        return 0;
    }
}
