// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Item { public int V; public Item(int v) { V = v; } }
class Program
{
    static int Main()
    {
        const int count = 200;
        var l = new List<Item>(count);
        for (int i = 0; i < count; i++) l.Add(new Item(i));
        if (l.Count != count) return 1;
        for (int i = 0; i < count; i++) if (l[i].V != i) return 1;
        Console.WriteLine("pal: alloc_many ok");
        return 0;
    }
}
