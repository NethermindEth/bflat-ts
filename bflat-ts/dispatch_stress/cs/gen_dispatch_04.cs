// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface I { int F(); }
class C4 : I { public int F() => 4; }


class Program
{
    static int Main()
    {
        I i = new C4();
        int s = 0; for (int k = 0; k < 50; k++) s += i.F();
        if (s != 200) return 1;
        Console.WriteLine("dispatch_stress: gen_dispatch_04 ok");
        return 0;
    }
}
