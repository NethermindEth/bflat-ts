// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Remove/re-add cycles over a grown table: bucket reuse after rehash.
using System;
using System.Collections;

class Program
{
    static int Main()
    {
        var ht = new Hashtable();
        for (int i = 0; i < 100; i++)
            ht.Add(i, i);
        for (int i = 0; i < 100; i += 2)
            ht.Remove(i);
        if (ht.Count != 50) return 1;
        for (int i = 0; i < 100; i += 2)
            ht.Add(i, i + 1000);
        if (ht.Count != 100) return 2;
        if ((int)ht[4] != 1004) return 3;
        if ((int)ht[5] != 5) return 4;
        Console.WriteLine("bcl_subst: ht_remove_readd ok");
        return 0;
    }
}
