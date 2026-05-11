// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Box { public int X; }
class Program
{
    static int Main()
    {
        Box last = null;
        for (int i = 0; i < 100; i++) last = new Box { X = i };
        if (last == null || last.X != 99) return 1;
        Console.WriteLine("memory: new_box_100 ok");
        return 0;
    }
}
