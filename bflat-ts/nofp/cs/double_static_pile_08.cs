// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static double D0 = 0.0; public static double D1 = 0.1; public static double D2 = 0.2; public static double D3 = 0.3; public static double D4 = 0.4; public static double D5 = 0.5; public static double D6 = 0.6; public static double D7 = 0.7; }


class Program
{
    static int Main()
    {
        double z = 0;
        z += Holder.D0;
        z += Holder.D1;
        z += Holder.D2;
        z += Holder.D3;
        z += Holder.D4;
        z += Holder.D5;
        z += Holder.D6;
        z += Holder.D7;
        GC.KeepAlive(z);
        Console.WriteLine("nofp: double_static_pile_08 ok");
        return 0;
    }
}
