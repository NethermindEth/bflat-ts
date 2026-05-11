// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static double D0 = 0.0; public static double D1 = 0.1; }


class Program
{
    static int Main()
    {
        double z = 0;
        z += Holder.D0;
        z += Holder.D1;
        GC.KeepAlive(z);
        Console.WriteLine("nofp: double_static_pile_02 ok");
        return 0;
    }
}
