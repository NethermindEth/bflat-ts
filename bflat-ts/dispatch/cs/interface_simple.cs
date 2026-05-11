// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface I { int F(); }
class C : I { public int F() => 7; }


class Program
{
    static int Main()
    {
        I i = new C();
        if (i.F() != 7) return 1;
        Console.WriteLine("dispatch: interface_simple ok");
        return 0;
    }
}
