// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        int[] a = new int[512];
        if (a.Length != 512) return 1;
        Console.WriteLine("memory: int_array_512 ok");
        return 0;
    }
}
