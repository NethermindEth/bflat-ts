// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Item { public int V; public Item(int v) { V = v; } }
class Program
{
    static int Main()
    {
        var l = new List<Item>(50);
        for (int i = 0; i < 50; i++) l.Add(new Item(i));
        if (l.Count != 50) return 1;
        for (int i = 0; i < 50; i++) if (l[i].V != i) return 1;
        Console.WriteLine("pal: alloc_many_50 ok");
        return 0;
    }
}
