// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        int[] a = new int[4];
        Array.Resize(ref a, 16);
        if (a.Length != 16) return 1;
        Console.WriteLine("memory: array_resize_4_16 ok");
        return 0;
    }
}
