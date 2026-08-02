// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static float F0 = 0.0f; }


class Program
{
    static int Main()
    {
        float z = 0;
        z += Holder.F0;
        GC.KeepAlive(z);
        Console.WriteLine("nofp: float_static_pile_01 ok");
        return 0;
    }
}
