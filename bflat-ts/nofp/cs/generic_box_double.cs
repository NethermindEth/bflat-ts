// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Box<T> { public T V; }
class Holder { public static double A = 1.5; }
class Program
{
    static int Main()
    {
        var b = new Box<double> { V = Holder.A };
        GC.KeepAlive(b);
        Console.WriteLine("nofp: generic_box_double ok");
        return 0;
    }
}
