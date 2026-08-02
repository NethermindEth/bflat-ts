// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        int[] a = new int[1024];
        if (a.Length != 1024) return 1;
        Console.WriteLine("memory: int_array_1024 ok");
        return 0;
    }
}
