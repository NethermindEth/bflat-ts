// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Same bit-pattern hashing contract for a float field.
using System;

struct FBox
{
    public float Value;
    public FBox(float v) => Value = v;
}

class Program
{
    static int Main()
    {
        var a = new FBox(3.25f);
        var b = new FBox(3.25f);
        var c = new FBox(-3.25f);
        if (a.GetHashCode() != b.GetHashCode()) return 1;
        if (a.GetHashCode() == c.GetHashCode()) return 2;
        if (!a.Equals(b)) return 3;
        if (a.Equals(c)) return 4;
        Console.WriteLine("bcl_subst: vt_hash_float_field ok");
        return 0;
    }
}
