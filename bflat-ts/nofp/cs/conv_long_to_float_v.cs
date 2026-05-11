// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static float F = 1.0f; public static double D = 1.0; }


class Program
{
    static int Main()
    {
        float y = (float)(5L);
        GC.KeepAlive(y);
        Console.WriteLine("nofp: conv_long_to_float_v ok");
        return 0;
    }
}
