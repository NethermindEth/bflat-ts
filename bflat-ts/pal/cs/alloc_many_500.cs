// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Item { public int V; public Item(int v) { V = v; } }
class Program
{
    static int Main()
    {
        var l = new List<Item>(500);
        for (int i = 0; i < 500; i++) l.Add(new Item(i));
        if (l.Count != 500) return 1;
        for (int i = 0; i < 500; i++) if (l[i].V != i) return 1;
        Console.WriteLine("pal: alloc_many_500 ok");
        return 0;
    }
}
