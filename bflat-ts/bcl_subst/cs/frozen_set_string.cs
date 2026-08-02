// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
// FrozenSet over strings: same frozen machinery on the set side.
using System;
using System.Collections.Frozen;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var src = new HashSet<string> { "red", "green", "blue", "cyan" };
        var f = src.ToFrozenSet();
        if (f.Count != 4) return 1;
        if (!f.Contains("red")) return 2;
        if (!f.Contains("cyan")) return 3;
        if (f.Contains("magenta")) return 4;
        Console.WriteLine("bcl_subst: frozen_set_string ok");
        return 0;
    }
}
