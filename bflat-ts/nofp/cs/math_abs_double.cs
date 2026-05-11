// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static double A = 1.5; }

class Program
{
    static int Main()
    {
        double z = Math.Abs(Holder.A);
        GC.KeepAlive(z);
        Console.WriteLine("nofp: math_abs_double ok");
        return 0;
    }
}
