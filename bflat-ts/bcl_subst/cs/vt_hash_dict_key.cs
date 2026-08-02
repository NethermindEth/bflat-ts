// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// A double-carrying struct as a Dictionary key: the hash/equals contract
// (bit-pattern hash + byte-wise ValueType.Equals) must round-trip lookups.
using System;
using System.Collections.Generic;

struct Point
{
    public double X;
    public double Y;
    public Point(double x, double y) { X = x; Y = y; }
}

class Program
{
    static int Main()
    {
        var d = new Dictionary<Point, int>();
        d[new Point(1.0, 2.0)] = 12;
        d[new Point(3.0, 4.0)] = 34;
        if (d.Count != 2) return 1;
        if (d[new Point(1.0, 2.0)] != 12) return 2;
        if (d[new Point(3.0, 4.0)] != 34) return 3;
        if (d.ContainsKey(new Point(1.0, 4.0))) return 4;
        d[new Point(1.0, 2.0)] = 99;
        if (d.Count != 2) return 5;
        if (d[new Point(1.0, 2.0)] != 99) return 6;
        Console.WriteLine("bcl_subst: vt_hash_dict_key ok");
        return 0;
    }
}
