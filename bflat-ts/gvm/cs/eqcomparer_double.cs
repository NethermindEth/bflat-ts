// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;



class Program
{
    static int Main()
    {
        if (!EqualityComparer<double>.Default.Equals(1.0, 1.0)) return 1;
        Console.WriteLine("gvm: eqcomparer_double ok");
        return 0;
    }
}
