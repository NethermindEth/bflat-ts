// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;



class Program
{
    static int Main()
    {
        if (!EqualityComparer<int>.Default.Equals(1, 1)) return 1;
        Console.WriteLine("gvm: eqcomparer_int ok");
        return 0;
    }
}
