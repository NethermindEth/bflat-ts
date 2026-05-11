// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static double A = 1.0; public static double B = 2.0; }

class Program
{
    static int Main()
    {
        bool r = Holder.A == Holder.B;
        GC.KeepAlive(r);
        Console.WriteLine("nofp: double_compare_eq ok");
        return 0;
    }
}
