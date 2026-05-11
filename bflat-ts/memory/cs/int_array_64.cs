// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        int[] a = new int[64];
        if (a.Length != 64) return 1;
        Console.WriteLine("memory: int_array_64 ok");
        return 0;
    }
}
