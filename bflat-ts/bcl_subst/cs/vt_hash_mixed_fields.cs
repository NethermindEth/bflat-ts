// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Mixed primitive + FP + reference fields walk every branch of the
// ValueTypeRegularHashCode snippet.
using System;

struct Mixed
{
    public int I;
    public double D;
    public string S;
    public Mixed(int i, double d, string s) { I = i; D = d; S = s; }
}

class Program
{
    static int Main()
    {
        var a = new Mixed(1, 2.5, "x");
        var b = new Mixed(1, 2.5, "x");
        var c = new Mixed(1, 3.5, "x");
        if (a.GetHashCode() != b.GetHashCode()) return 1;
        if (!a.Equals(b)) return 2;
        if (a.Equals(c)) return 3;
        Console.WriteLine("bcl_subst: vt_hash_mixed_fields ok");
        return 0;
    }
}
