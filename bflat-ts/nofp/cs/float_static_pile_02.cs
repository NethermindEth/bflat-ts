// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static float F0 = 0.0f; public static float F1 = 0.1f; }


class Program
{
    static int Main()
    {
        float z = 0;
        z += Holder.F0;
        z += Holder.F1;
        GC.KeepAlive(z);
        Console.WriteLine("nofp: float_static_pile_02 ok");
        return 0;
    }
}
