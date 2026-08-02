// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// Structs with a double field hash via the raw bit pattern on zisk
// (ValueTypeRegularHashCode snippet). Equal values must hash equal.
using System;

struct DBox
{
    public double Value;
    public DBox(double v) => Value = v;
}

class Program
{
    static int Main()
    {
        var a = new DBox(1.5);
        var b = new DBox(1.5);
        var c = new DBox(2.5);
        if (a.GetHashCode() != b.GetHashCode()) return 1;
        if (a.GetHashCode() == c.GetHashCode()) return 2;
        if (!a.Equals(b)) return 3;
        if (a.Equals(c)) return 4;
        Console.WriteLine("bcl_subst: vt_hash_double_field ok");
        return 0;
    }
}
