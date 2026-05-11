// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static float F = 1.0f; public static double D = 1.0; }


class Program
{
    static int Main()
    {
        long y = (long)(Holder.D);
        GC.KeepAlive(y);
        Console.WriteLine("nofp: conv_double_to_long_v ok");
        return 0;
    }
}
