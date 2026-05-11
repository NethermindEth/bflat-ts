// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static float A = 1.0f; public static float B = 2.0f; }


class Program
{
    static int Main()
    {
        float z0 = Holder.A + Holder.B; GC.KeepAlive(z0);
        float z1 = Holder.A * Holder.B; GC.KeepAlive(z1);
        float z2 = Holder.A + Holder.B; GC.KeepAlive(z2);
        float z3 = Holder.A * Holder.B; GC.KeepAlive(z3);
        Console.WriteLine("nofp: float_chain_indep_04 ok");
        return 0;
    }
}
