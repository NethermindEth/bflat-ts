// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static float F = 1.0f; public static double D = 1.0; }


class Program
{
    static int Main()
    {
        int y = (int)(Holder.D);
        GC.KeepAlive(y);
        Console.WriteLine("nofp: conv_double_to_int_v ok");
        return 0;
    }
}
