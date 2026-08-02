// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// DictionaryEntry enumeration must see every entry exactly once.
using System;
using System.Collections;

class Program
{
    static int Main()
    {
        var ht = new Hashtable();
        for (int i = 0; i < 20; i++)
            ht.Add(i, i * 7);
        int count = 0;
        long sum = 0;
        foreach (DictionaryEntry e in ht)
        {
            count++;
            sum += (int)e.Value - (int)e.Key * 7;
        }
        if (count != 20) return 1;
        if (sum != 0) return 2;
        Console.WriteLine("bcl_subst: ht_iterate_entries ok");
        return 0;
    }
}
