// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// The (comparer) ctor snippet must keep the custom IEqualityComparer.
using System;
using System.Collections;

class Program
{
    static int Main()
    {
        var ht = new Hashtable(StringComparer.OrdinalIgnoreCase);
        ht.Add("Key", 1);
        if (!ht.ContainsKey("KEY")) return 1;
        if (!ht.ContainsKey("key")) return 2;
        if ((int)ht["kEy"] != 1) return 3;
        ht["KEY"] = 2;
        if (ht.Count != 1) return 4;
        if ((int)ht["Key"] != 2) return 5;
        Console.WriteLine("bcl_subst: ht_comparer_case_insensitive ok");
        return 0;
    }
}
