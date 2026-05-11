// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        int n = Environment.ProcessorCount;
        if (n != 1) return 1;
        Console.WriteLine("pal: processor_count ok");
        return 0;
    }
}
