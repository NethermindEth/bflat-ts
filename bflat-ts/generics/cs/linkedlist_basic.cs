// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var l = new LinkedList<int>();
        l.AddLast(1); l.AddLast(2); l.AddFirst(0);
        if (l.Count != 3) return 1;
        if (l.First.Value != 0) return 1;
        if (l.Last.Value != 2) return 1;
        Console.WriteLine("generics: linkedlist_basic ok");
        return 0;
    }
}
