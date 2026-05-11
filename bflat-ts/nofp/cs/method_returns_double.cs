// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static double Get() => 1.5;
    static int Main()
    {
        double v = Get();
        GC.KeepAlive(v);
        Console.WriteLine("nofp: method_returns_double ok");
        return 0;
    }
}
