// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Bit-pattern hashing pairs with NativeAOT's byte-wise ValueType.Equals:
// +0.0 and -0.0 have different bit patterns, so as struct fields they are
// different keys AND different hashes - the contract stays consistent.
using System;
using System.Collections.Generic;

struct DBox
{
    public double Value;
    public DBox(double v) => Value = v;
}

class Program
{
    static int Main()
    {
        var pos = new DBox(0.0);
        var neg = new DBox(-0.0);
        bool equal = pos.Equals(neg);
        bool sameHash = pos.GetHashCode() == neg.GetHashCode();
        if (equal != sameHash) return 1;
        var d = new Dictionary<DBox, int>();
        d[pos] = 1;
        if (d.ContainsKey(neg) != equal) return 2;
        Console.WriteLine("bcl_subst: vt_hash_negzero_distinct ok");
        return 0;
    }
}
