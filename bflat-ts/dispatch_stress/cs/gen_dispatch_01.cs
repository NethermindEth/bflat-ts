// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface I { int F(); }
class C1 : I { public int F() => 1; }


class Program
{
    static int Main()
    {
        I i = new C1();
        int s = 0; for (int k = 0; k < 50; k++) s += i.F();
        if (s != 50) return 1;
        Console.WriteLine("dispatch_stress: gen_dispatch_01 ok");
        return 0;
    }
}
