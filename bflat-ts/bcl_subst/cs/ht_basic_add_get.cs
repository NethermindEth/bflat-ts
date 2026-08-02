// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Hashtable default ctor is replaced by an integer-sizing snippet on zisk:
// basic add/get/contains/remove/count must behave normally.
using System;
using System.Collections;

class Program
{
    static int Main()
    {
        var ht = new Hashtable();
        ht.Add("one", 1);
        ht.Add("two", 2);
        ht["three"] = 3;
        if (ht.Count != 3) return 1;
        if ((int)ht["one"] != 1) return 2;
        if ((int)ht["three"] != 3) return 3;
        if (!ht.ContainsKey("two")) return 4;
        if (ht.ContainsKey("four")) return 5;
        ht.Remove("two");
        if (ht.Count != 2) return 6;
        if (ht["two"] != null) return 7;
        Console.WriteLine("bcl_subst: ht_basic_add_get ok");
        return 0;
    }
}
