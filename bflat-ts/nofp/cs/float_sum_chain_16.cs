// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static float F0 = 1f; public static float F1 = 2f; public static float F2 = 3f; public static float F3 = 4f; public static float F4 = 5f; public static float F5 = 6f; public static float F6 = 7f; public static float F7 = 8f; public static float F8 = 9f; public static float F9 = 10f; public static float F10 = 11f; public static float F11 = 12f; public static float F12 = 13f; public static float F13 = 14f; public static float F14 = 15f; public static float F15 = 16f; }


class Program
{
    static int Main()
    {
        float z = Holder.F0 + Holder.F1 + Holder.F2 + Holder.F3 + Holder.F4 + Holder.F5 + Holder.F6 + Holder.F7 + Holder.F8 + Holder.F9 + Holder.F10 + Holder.F11 + Holder.F12 + Holder.F13 + Holder.F14 + Holder.F15;
        GC.KeepAlive(z);
        Console.WriteLine("nofp: float_sum_chain_16 ok");
        return 0;
    }
}
