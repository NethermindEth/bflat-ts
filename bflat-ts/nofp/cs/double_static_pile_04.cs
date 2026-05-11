// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static double D0 = 0.0; public static double D1 = 0.1; public static double D2 = 0.2; public static double D3 = 0.3; }


class Program
{
    static int Main()
    {
        double z = 0;
        z += Holder.D0;
        z += Holder.D1;
        z += Holder.D2;
        z += Holder.D3;
        GC.KeepAlive(z);
        Console.WriteLine("nofp: double_static_pile_04 ok");
        return 0;
    }
}
