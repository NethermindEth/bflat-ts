// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// A nested struct containing a double recurses through the boxed-field
// branch of the hashing snippet.
using System;

struct Inner
{
    public double V;
    public Inner(double v) => V = v;
}

struct Outer
{
    public Inner In;
    public int Tag;
    public Outer(double v, int tag) { In = new Inner(v); Tag = tag; }
}

class Program
{
    static int Main()
    {
        var a = new Outer(9.75, 5);
        var b = new Outer(9.75, 5);
        var c = new Outer(8.75, 5);
        if (a.GetHashCode() != b.GetHashCode()) return 1;
        if (!a.Equals(b)) return 2;
        if (a.Equals(c)) return 3;
        Console.WriteLine("bcl_subst: vt_hash_nested_struct ok");
        return 0;
    }
}
