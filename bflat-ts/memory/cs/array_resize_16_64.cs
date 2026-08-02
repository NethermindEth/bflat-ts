// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        int[] a = new int[16];
        Array.Resize(ref a, 64);
        if (a.Length != 64) return 1;
        Console.WriteLine("memory: array_resize_16_64 ok");
        return 0;
    }
}
