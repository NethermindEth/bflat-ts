// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static float A = 1.0f; public static float B = 2.0f; }

class Program
{
    static int Main()
    {
        bool r = Holder.A >= Holder.B;
        GC.KeepAlive(r);
        Console.WriteLine("nofp: float_compare_ge ok");
        return 0;
    }
}
