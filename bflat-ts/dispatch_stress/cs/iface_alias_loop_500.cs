// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface I { int F(); }
class C : I { public int F() => 1; }


class Program
{
    static int Main()
    {
        I i = new C();
        int s = 0; for (int k = 0; k < 500; k++) s += i.F();
        if (s != 500) return 1;
        Console.WriteLine("dispatch_stress: iface_alias_loop_500 ok");
        return 0;
    }
}
