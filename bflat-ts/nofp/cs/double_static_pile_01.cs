// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static double D0 = 0.0; }


class Program
{
    static int Main()
    {
        double z = 0;
        z += Holder.D0;
        GC.KeepAlive(z);
        Console.WriteLine("nofp: double_static_pile_01 ok");
        return 0;
    }
}
