// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Holder { public static float A = 1.0f; }

class Program
{
    static int Main()
    {
        object o = (float)Holder.A;
        GC.KeepAlive(o);
        Console.WriteLine("nofp: box_float ok");
        return 0;
    }
}
