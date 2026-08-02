// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static double[] s_a = new double[16];
    static int Main()
    {
        for (int i = 0; i < 16; i++) s_a[i] = i;
        double sum = 0;
        for (int i = 0; i < 16; i++) sum += s_a[i];
        GC.KeepAlive(sum);
        Console.WriteLine("nofp: double_array_sum_16 ok");
        return 0;
    }
}
