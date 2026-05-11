// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface I { int F(); }
class C1 : I { public int F() => 1; }
class C2 : I { public int F() => 2; }

class Program
{
    static int Main()
    {
        {I i = new C1(); if (i.F() != 1) return 1;}
        {I i = new C2(); if (i.F() != 2) return 1;}
        Console.WriteLine("dispatch: interface_2_impl ok");
        return 0;
    }
}
