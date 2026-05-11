// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static float A = 1.0f; public static float B = 2.0f; }


class Program
{
    static int Main()
    {
        float z = Holder.A;
        z -= Holder.B;
        GC.KeepAlive(z);
        Console.WriteLine("nofp: float_sub_assign ok");
        return 0;
    }
}
