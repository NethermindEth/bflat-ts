// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface I { int F(); }
class C3 : I { public int F() => 3; }


class Program
{
    static int Main()
    {
        I i = new C3();
        int s = 0; for (int k = 0; k < 50; k++) s += i.F();
        if (s != 150) return 1;
        Console.WriteLine("dispatch_stress: gen_dispatch_03 ok");
        return 0;
    }
}
