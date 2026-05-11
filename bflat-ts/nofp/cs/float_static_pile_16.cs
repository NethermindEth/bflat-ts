// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static float F0 = 0.0f; public static float F1 = 0.1f; public static float F2 = 0.2f; public static float F3 = 0.3f; public static float F4 = 0.4f; public static float F5 = 0.5f; public static float F6 = 0.6f; public static float F7 = 0.7f; public static float F8 = 0.8f; public static float F9 = 0.9f; public static float F10 = 1.0f; public static float F11 = 1.1f; public static float F12 = 1.2f; public static float F13 = 1.3f; public static float F14 = 1.4f; public static float F15 = 1.5f; }


class Program
{
    static int Main()
    {
        float z = 0;
        z += Holder.F0;
        z += Holder.F1;
        z += Holder.F2;
        z += Holder.F3;
        z += Holder.F4;
        z += Holder.F5;
        z += Holder.F6;
        z += Holder.F7;
        z += Holder.F8;
        z += Holder.F9;
        z += Holder.F10;
        z += Holder.F11;
        z += Holder.F12;
        z += Holder.F13;
        z += Holder.F14;
        z += Holder.F15;
        GC.KeepAlive(z);
        Console.WriteLine("nofp: float_static_pile_16 ok");
        return 0;
    }
}
