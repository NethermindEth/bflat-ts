// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface I { int F(); }
class C2 : I { public int F() => 2; }


class Program
{
    static int Main()
    {
        I i = new C2();
        int s = 0; for (int k = 0; k < 50; k++) s += i.F();
        if (s != 100) return 1;
        Console.WriteLine("dispatch_stress: gen_dispatch_02 ok");
        return 0;
    }
}
