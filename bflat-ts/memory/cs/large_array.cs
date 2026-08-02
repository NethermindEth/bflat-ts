// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        int[] a = new int[100000];
        a[0] = 1; a[99999] = 2;
        if (a[0] + a[99999] != 3) return 1;
        Console.WriteLine("memory: large_array ok");
        return 0;
    }
}
