// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Big { public int[] Data = new int[4]; }
class Program
{
    static int Main()
    {
        var b = new Big();
        if (b.Data.Length != 4) return 1;
        Console.WriteLine("pal: alloc_size_4 ok");
        return 0;
    }
}
