// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;



class Program
{
    static int Main()
    {
        if (!EqualityComparer<bool>.Default.Equals(true, true)) return 1;
        Console.WriteLine("gvm: eqcomparer_bool ok");
        return 0;
    }
}
