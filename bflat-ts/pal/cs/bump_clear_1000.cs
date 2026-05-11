// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        var l = new List<object>();
        for (int i = 0; i < 1000; i++) l.Add(new object());
        if (l.Count != 1000) return 1;
        l.Clear();
        if (l.Count != 0) return 1;
        Console.WriteLine("pal: bump_clear_1000 ok");
        return 0;
    }
}
