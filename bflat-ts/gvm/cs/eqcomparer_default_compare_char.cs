// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;



class Program
{
    static int Main()
    {
        var cmp = EqualityComparer<char>.Default;
        if (cmp == null) return 1;
        Console.WriteLine("gvm: eqcomparer_default_compare_char ok");
        return 0;
    }
}
