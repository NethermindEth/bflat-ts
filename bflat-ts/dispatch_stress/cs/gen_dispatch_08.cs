// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface I { int F(); }
class C8 : I { public int F() => 8; }


class Program
{
    static int Main()
    {
        I i = new C8();
        int s = 0; for (int k = 0; k < 50; k++) s += i.F();
        if (s != 400) return 1;
        Console.WriteLine("dispatch_stress: gen_dispatch_08 ok");
        return 0;
    }
}
