// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface I { int F(); }
class C7 : I { public int F() => 7; }


class Program
{
    static int Main()
    {
        I i = new C7();
        int s = 0; for (int k = 0; k < 50; k++) s += i.F();
        if (s != 350) return 1;
        Console.WriteLine("dispatch_stress: gen_dispatch_07 ok");
        return 0;
    }
}
