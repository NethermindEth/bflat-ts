// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static double A = 0.5; }

class Program
{
    static int Main()
    {
        int i = 4;
        double d = i;
        double z = d * Holder.A;
        GC.KeepAlive(z);
        Console.WriteLine("nofp: int_to_double_then_mul ok");
        return 0;
    }
}
