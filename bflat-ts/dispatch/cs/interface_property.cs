// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface I { int X { get; } }
class C : I { public int X => 7; }


class Program
{
    static int Main()
    {
        I i = new C();
        if (i.X != 7) return 1;
        Console.WriteLine("dispatch: interface_property ok");
        return 0;
    }
}
