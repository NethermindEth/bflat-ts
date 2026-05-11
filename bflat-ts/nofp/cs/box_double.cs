// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static double A = 1.0; }

class Program
{
    static int Main()
    {
        object o = (double)Holder.A;
        GC.KeepAlive(o);
        Console.WriteLine("nofp: box_double ok");
        return 0;
    }
}
