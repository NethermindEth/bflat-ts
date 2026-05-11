// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static double A = 1.0; public static double B = 2.0; }


class Program
{
    static int Main()
    {
        double z = Holder.A;
        z *= Holder.B;
        GC.KeepAlive(z);
        Console.WriteLine("nofp: double_mul_assign ok");
        return 0;
    }
}
