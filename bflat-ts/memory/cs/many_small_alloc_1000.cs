// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        object[] objs = new object[1000];
        for (int i = 0; i < 1000; i++) objs[i] = new object();
        int nn = 0; foreach (var o in objs) if (o != null) nn++;
        if (nn != 1000) return 1;
        Console.WriteLine("memory: many_small_alloc_1000 ok");
        return 0;
    }
}
