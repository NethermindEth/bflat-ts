// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Text;
using System.Collections.Generic;

class Box
{
    public int F0;
    public int F1;
    public int F2;
    public int F3;
}
class Program
{
    static int Main()
    {
        var b = new Box();
        b.F0 = 1;
        b.F3 = 2;
        if (b.F0 + b.F3 != 3) return 1;
        Console.WriteLine("memory: box_fields_4 ok");
        return 0;
    }
}
