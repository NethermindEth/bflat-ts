// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static float A = 1.5f; public static float B = 2.5f; }


class Program
{
    static int Main()
    {
        float z = Holder.A - Holder.B;
        GC.KeepAlive(z);
        Console.WriteLine("nofp: float_sub_static ok");
        return 0;
    }
}
