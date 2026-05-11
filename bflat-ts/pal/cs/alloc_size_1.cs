// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Big { public int[] Data = new int[1]; }
class Program
{
    static int Main()
    {
        var b = new Big();
        if (b.Data.Length != 1) return 1;
        Console.WriteLine("pal: alloc_size_1 ok");
        return 0;
    }
}
