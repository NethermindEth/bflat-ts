// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;



class Program
{
    static int Main()
    {
        if (!EqualityComparer<char>.Default.Equals('a', 'a')) return 1;
        Console.WriteLine("gvm: eqcomparer_char ok");
        return 0;
    }
}
