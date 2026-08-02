// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;



class Program
{
    static int Main()
    {
        if (!EqualityComparer<string>.Default.Equals("x", "x")) return 1;
        Console.WriteLine("gvm: eqcomparer_string ok");
        return 0;
    }
}
