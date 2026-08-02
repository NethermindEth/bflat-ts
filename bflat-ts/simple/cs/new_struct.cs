// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

struct P { public int X, Y; }
class Program
{
    static int Main()
    {
        var p = new P { X = 1, Y = 2 };
        Console.WriteLine(p.X + p.Y);
        return 0;
    }
}
