// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static readonly object _lock = new();
    static int Main()
    {
        lock (_lock)
        {
            Console.WriteLine("pal: lock_basic ok");
        }
        return 0;
    }
}
