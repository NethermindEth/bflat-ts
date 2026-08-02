// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// FrozenDictionary over int keys hits the CalcNumBuckets wrap without the
// string-specific paths.
using System;
using System.Collections.Frozen;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var src = new Dictionary<int, long>();
        for (int i = 0; i < 64; i++)
            src[i * 3] = (long)i * 100;
        var f = src.ToFrozenDictionary();
        if (f.Count != 64) return 1;
        for (int i = 0; i < 64; i++)
            if (f[i * 3] != (long)i * 100) return 2;
        if (f.ContainsKey(1)) return 3;
        Console.WriteLine("bcl_subst: frozen_dict_int ok");
        return 0;
    }
}
