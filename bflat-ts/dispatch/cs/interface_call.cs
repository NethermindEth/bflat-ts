// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface I { int F(); } class C : I { public int F() => 42; }

class Program
{
    static int Main()
    {
        I i = new C();
        if (i.F() != 42) return 1;
        Console.WriteLine("dispatch: interface_call ok");
        return 0;
    }
}
