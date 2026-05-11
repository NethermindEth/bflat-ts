// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        object[] a = new object[32];
        for (int i = 0; i < 32; i++) a[i] = i;
        if ((int)a[31] != 31) return 1;
        Console.WriteLine("memory: obj_array_32 ok");
        return 0;
    }
}
