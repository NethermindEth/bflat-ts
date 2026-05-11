// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;



class Program
{
    static int Main()
    {
        if (!EqualityComparer<long>.Default.Equals(1L, 1L)) return 1;
        Console.WriteLine("gvm: eqcomparer_long ok");
        return 0;
    }
}
