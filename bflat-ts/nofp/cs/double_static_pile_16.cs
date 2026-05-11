// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static double D0 = 0.0; public static double D1 = 0.1; public static double D2 = 0.2; public static double D3 = 0.3; public static double D4 = 0.4; public static double D5 = 0.5; public static double D6 = 0.6; public static double D7 = 0.7; public static double D8 = 0.8; public static double D9 = 0.9; public static double D10 = 1.0; public static double D11 = 1.1; public static double D12 = 1.2; public static double D13 = 1.3; public static double D14 = 1.4; public static double D15 = 1.5; }


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
        z += Holder.D8;
        z += Holder.D9;
        z += Holder.D10;
        z += Holder.D11;
        z += Holder.D12;
        z += Holder.D13;
        z += Holder.D14;
        z += Holder.D15;
        GC.KeepAlive(z);
        Console.WriteLine("nofp: double_static_pile_16 ok");
        return 0;
    }
}
