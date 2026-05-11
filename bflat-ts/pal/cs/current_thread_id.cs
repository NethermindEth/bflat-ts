// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        int id = Environment.CurrentManagedThreadId;
        GC.KeepAlive(id);
        Console.WriteLine("pal: current_thread_id ok");
        return 0;
    }
}
