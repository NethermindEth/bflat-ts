// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Item { public int V; }
class Program
{
    static int Main()
    {
        var x = new Item { V = 7 };
        if (x.V != 7) return 1;
        Console.WriteLine("pal: obj_init_field ok");
        return 0;
    }
}
