// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class H<T> { [ThreadStatic] public static int V; }
class Program
{
    static int Main()
    {
        H<int>.V = 7;
        H<string>.V = 14;
        if (H<int>.V != 7) return 1;
        if (H<string>.V != 14) return 1;
        Console.WriteLine("tls: generic_class_ts_int ok");
        return 0;
    }
}
