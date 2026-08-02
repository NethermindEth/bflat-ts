// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Base {}
class D : Base { public int X = 7; }


class Program
{
    static int Main()
    {
        Base b = new D();
        var d = b as D;
        if (d == null || d.X != 7) return 1;
        Console.WriteLine("dispatch: as_cast ok");
        return 0;
    }
}
