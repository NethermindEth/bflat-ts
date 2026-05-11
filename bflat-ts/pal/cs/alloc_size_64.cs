// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Big { public int[] Data = new int[64]; }
class Program
{
    static int Main()
    {
        var b = new Big();
        if (b.Data.Length != 64) return 1;
        Console.WriteLine("pal: alloc_size_64 ok");
        return 0;
    }
}
