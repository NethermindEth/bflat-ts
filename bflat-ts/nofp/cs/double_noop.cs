// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static double s_x = 1.5, s_y = 2.5;
    static int Main()
    {
        double z = s_x + s_y;
        double w = s_x * s_y;
        GC.KeepAlive(z); GC.KeepAlive(w);
        Console.WriteLine("nofp: double_noop ok");
        return 0;
    }
}
