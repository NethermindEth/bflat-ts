// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Box<T> { public T V; }
class Holder { public static float A = 1.5f; }
class Program
{
    static int Main()
    {
        var b = new Box<float> { V = Holder.A };
        GC.KeepAlive(b);
        Console.WriteLine("nofp: generic_box_float ok");
        return 0;
    }
}
