// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static float A = 0.5f; }

class Program
{
    static int Main()
    {
        int i = 4;
        float f = i;
        float z = f * Holder.A;
        GC.KeepAlive(z);
        Console.WriteLine("nofp: int_to_float_then_mul ok");
        return 0;
    }
}
