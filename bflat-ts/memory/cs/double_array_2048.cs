// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        double[] a = new double[2048];
        if (a.Length != 2048) return 1;
        Console.WriteLine("memory: double_array_2048 ok");
        return 0;
    }
}
