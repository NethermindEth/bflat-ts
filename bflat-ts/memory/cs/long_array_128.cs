// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        long[] a = new long[128];
        if (a.Length != 128) return 1;
        Console.WriteLine("memory: long_array_128 ok");
        return 0;
    }
}
