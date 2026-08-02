// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface I { int F(); }
class C9 : I { public int F() => 9; }


class Program
{
    static int Main()
    {
        I i = new C9();
        int s = 0; for (int k = 0; k < 50; k++) s += i.F();
        if (s != 450) return 1;
        Console.WriteLine("dispatch_stress: gen_dispatch_09 ok");
        return 0;
    }
}
