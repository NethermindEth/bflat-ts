// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static float F0 = 1f; public static float F1 = 2f; public static float F2 = 3f; public static float F3 = 4f; }


class Program
{
    static int Main()
    {
        float z = Holder.F0 + Holder.F1 + Holder.F2 + Holder.F3;
        GC.KeepAlive(z);
        Console.WriteLine("nofp: float_sum_chain_4 ok");
        return 0;
    }
}
