// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static double A = 1.5; public static double B = 2.5; }


class Program
{
    static int Main()
    {
        double z = Holder.A - Holder.B;
        GC.KeepAlive(z);
        Console.WriteLine("nofp: double_sub_static ok");
        return 0;
    }
}
