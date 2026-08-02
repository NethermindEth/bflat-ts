// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        long[] a = new long[8];
        if (a.Length != 8) return 1;
        Console.WriteLine("memory: long_array_8 ok");
        return 0;
    }
}
