// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        long[] a = new long[32];
        if (a.Length != 32) return 1;
        Console.WriteLine("memory: long_array_32 ok");
        return 0;
    }
}
