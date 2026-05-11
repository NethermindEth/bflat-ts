// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        object[] a = new object[512];
        for (int i = 0; i < 512; i++) a[i] = i;
        if ((int)a[511] != 511) return 1;
        Console.WriteLine("memory: obj_array_512 ok");
        return 0;
    }
}
