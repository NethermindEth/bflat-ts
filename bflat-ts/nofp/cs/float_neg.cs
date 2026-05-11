// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static float A = 1.5f; }

class Program
{
    static int Main()
    {
        float z = -Holder.A;
        GC.KeepAlive(z);
        Console.WriteLine("nofp: float_neg ok");
        return 0;
    }
}
