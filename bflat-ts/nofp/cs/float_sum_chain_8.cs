// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static float F0 = 1f; public static float F1 = 2f; public static float F2 = 3f; public static float F3 = 4f; public static float F4 = 5f; public static float F5 = 6f; public static float F6 = 7f; public static float F7 = 8f; }


class Program
{
    static int Main()
    {
        float z = Holder.F0 + Holder.F1 + Holder.F2 + Holder.F3 + Holder.F4 + Holder.F5 + Holder.F6 + Holder.F7;
        GC.KeepAlive(z);
        Console.WriteLine("nofp: float_sum_chain_8 ok");
        return 0;
    }
}
