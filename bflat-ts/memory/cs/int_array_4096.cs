// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        int[] a = new int[4096];
        if (a.Length != 4096) return 1;
        Console.WriteLine("memory: int_array_4096 ok");
        return 0;
    }
}
