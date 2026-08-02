// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// FrozenDictionary over string keys: creation rates bucket counts via the
// wrapped CalcNumBuckets and the LengthBuckets snippet (always fallback).
using System;
using System.Collections.Frozen;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var src = new Dictionary<string, int>
        {
            ["alpha"] = 1, ["beta"] = 2, ["gamma"] = 3,
            ["delta"] = 4, ["epsilon"] = 5,
        };
        var f = src.ToFrozenDictionary();
        if (f.Count != 5) return 1;
        if (f["alpha"] != 1) return 2;
        if (f["epsilon"] != 5) return 3;
        if (f.ContainsKey("zeta")) return 4;
        if (!f.TryGetValue("gamma", out int g) || g != 3) return 5;
        Console.WriteLine("bcl_subst: frozen_dict_string ok");
        return 0;
    }
}
