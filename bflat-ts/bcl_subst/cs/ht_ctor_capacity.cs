// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// The capacity ctor snippet sizes buckets as GetPrime(capacity*100/72);
// filling past the initial capacity must grow correctly.
using System;
using System.Collections;

class Program
{
    static int Main()
    {
        var ht = new Hashtable(16);
        for (int i = 0; i < 64; i++)
            ht.Add(i, -i);
        if (ht.Count != 64) return 1;
        for (int i = 0; i < 64; i++)
            if ((int)ht[i] != -i) return 2;
        var empty = new Hashtable(0);
        empty.Add("a", 1);
        if ((int)empty["a"] != 1) return 3;
        Console.WriteLine("bcl_subst: ht_ctor_capacity ok");
        return 0;
    }
}
