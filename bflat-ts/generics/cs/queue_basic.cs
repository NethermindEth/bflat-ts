// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var q = new Queue<int>();
        q.Enqueue(1); q.Enqueue(2);
        if (q.Dequeue() != 1) return 1;
        if (q.Dequeue() != 2) return 1;
        Console.WriteLine("generics: queue_basic ok");
        return 0;
    }
}
