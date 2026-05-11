// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Program
{
    static float Get() => 1.5f;
    static int Main()
    {
        float v = Get();
        GC.KeepAlive(v);
        Console.WriteLine("nofp: method_returns_float ok");
        return 0;
    }
}
