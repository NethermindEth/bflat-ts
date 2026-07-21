// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Exercises: Dictionary<string,int> Add, TryGetValue, ContainsKey, overwrite, Count
// Bug targeted: hashtable rehashing + realloc in managed heap

using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var d = new Dictionary<string, int>();

        d.Add("one",   1);
        d.Add("two",   2);
        d.Add("three", 3);
        d.Add("four",  4);
        d.Add("five",  5);

        if (d.Count != 5) return 1;

        // TryGetValue for every initial key
        int val;
        if (!d.TryGetValue("one",   out val) || val != 1) return 1;
        if (!d.TryGetValue("two",   out val) || val != 2) return 1;
        if (!d.TryGetValue("three", out val) || val != 3) return 1;
        if (!d.TryGetValue("four",  out val) || val != 4) return 1;
        if (!d.TryGetValue("five",  out val) || val != 5) return 1;

        // Overwrite an existing key
        d["three"] = 33;
        if (d["three"] != 33) return 1;
        if (d["one"]   != 1)  return 1;
        if (d.Count    != 5)  return 1;

        // Missing key must not be found
        if (d.ContainsKey("six")) return 1;

        // Force rehashing by adding 20 more entries
        for (int i = 0; i < 20; i++)
            d.Add($"key{i}", i);

        if (d.Count != 25) return 1;

        // Spot-check a few of the bulk-added entries
        if (!d.TryGetValue("key0",  out val) || val != 0)  return 1;
        if (!d.TryGetValue("key9",  out val) || val != 9)  return 1;
        if (!d.TryGetValue("key19", out val) || val != 19) return 1;

        // Original entries still intact after rehash
        if (d["one"]   != 1)  return 1;
        if (d["three"] != 33) return 1;

        Console.WriteLine("generics: dict_basic ok count=25");
        return 0;
    }
}