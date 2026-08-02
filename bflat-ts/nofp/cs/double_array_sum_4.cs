// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static double[] s_a = new double[4];
    static int Main()
    {
        for (int i = 0; i < 4; i++) s_a[i] = i;
        double sum = 0;
        for (int i = 0; i < 4; i++) sum += s_a[i];
        GC.KeepAlive(sum);
        Console.WriteLine("nofp: double_array_sum_4 ok");
        return 0;
    }
}
