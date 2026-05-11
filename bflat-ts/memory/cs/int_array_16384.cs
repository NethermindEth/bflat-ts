// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        int[] a = new int[16384];
        if (a.Length != 16384) return 1;
        Console.WriteLine("memory: int_array_16384 ok");
        return 0;
    }
}
