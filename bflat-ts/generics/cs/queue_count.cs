// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var q = new Queue<int>();
        for (int i = 0; i < 5; i++) q.Enqueue(i);
        if (q.Count != 5) return 1;
        Console.WriteLine("generics: queue_count ok");
        return 0;
    }
}
