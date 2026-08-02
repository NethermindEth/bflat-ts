// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static float A = 1.0f; public static float B = 2.0f; }


class Program
{
    static int Main()
    {
        float z0 = Holder.A + Holder.B; GC.KeepAlive(z0);
        Console.WriteLine("nofp: float_chain_indep_01 ok");
        return 0;
    }
}
