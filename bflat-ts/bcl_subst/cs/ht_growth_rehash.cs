// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Insert enough entries to force several rehash() cycles (integer
// newsize*72/100 load-size math + wrapped GetPrime/IsPrime).
using System;
using System.Collections;

class Program
{
    static int Main()
    {
        var ht = new Hashtable();
        for (int i = 0; i < 500; i++)
            ht.Add(i, i * 3);
        if (ht.Count != 500) return 1;
        for (int i = 0; i < 500; i++)
            if ((int)ht[i] != i * 3) return 2;
        if (ht.ContainsKey(500)) return 3;
        Console.WriteLine("bcl_subst: ht_growth_rehash ok");
        return 0;
    }
}
