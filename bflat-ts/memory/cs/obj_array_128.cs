// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        object[] a = new object[128];
        for (int i = 0; i < 128; i++) a[i] = i;
        if ((int)a[127] != 127) return 1;
        Console.WriteLine("memory: obj_array_128 ok");
        return 0;
    }
}
