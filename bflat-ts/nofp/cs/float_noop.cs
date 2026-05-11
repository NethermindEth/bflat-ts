// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static float s_x = 1.5f, s_y = 2.5f;
    static int Main()
    {
        float z = s_x + s_y;
        float w = s_x * s_y;
        GC.KeepAlive(z); GC.KeepAlive(w);
        Console.WriteLine("nofp: float_noop ok");
        return 0;
    }
}
