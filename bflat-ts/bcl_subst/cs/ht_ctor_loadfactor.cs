// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// The (capacity, loadFactor) ctor is replaced by a snippet that fixes the
// load factor at 0.72; a custom factor must still yield a working table.
using System;
using System.Collections;

class Program
{
    static int Main()
    {
        var ht = new Hashtable(10, 0.9f);
        for (int i = 0; i < 100; i++)
            ht.Add("k" + i, i);
        if (ht.Count != 100) return 1;
        for (int i = 0; i < 100; i++)
            if ((int)ht["k" + i] != i) return 2;
        Console.WriteLine("bcl_subst: ht_ctor_loadfactor ok");
        return 0;
    }
}
