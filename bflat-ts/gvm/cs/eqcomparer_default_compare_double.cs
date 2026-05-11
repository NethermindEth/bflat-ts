// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;



class Program
{
    static int Main()
    {
        var cmp = EqualityComparer<double>.Default;
        if (cmp == null) return 1;
        Console.WriteLine("gvm: eqcomparer_default_compare_double ok");
        return 0;
    }
}
