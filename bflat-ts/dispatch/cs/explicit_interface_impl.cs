// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface I { int F(); }
class C : I { int I.F() => 7; }


class Program
{
    static int Main()
    {
        I i = new C();
        if (i.F() != 7) return 1;
        Console.WriteLine("dispatch: explicit_interface_impl ok");
        return 0;
    }
}
