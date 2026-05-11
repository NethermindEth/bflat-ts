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
        for (int i = 0; i < 1000; i++) last = new Box { X = i };
        if (last == null || last.X != 999) return 1;
        Console.WriteLine("memory: new_box_1000 ok");
        return 0;
    }
}
