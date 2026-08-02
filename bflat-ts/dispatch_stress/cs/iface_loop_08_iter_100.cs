// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface I { int F(); }
class C0 : I { public int F() => 1; }
class C1 : I { public int F() => 1; }
class C2 : I { public int F() => 1; }
class C3 : I { public int F() => 1; }
class C4 : I { public int F() => 1; }
class C5 : I { public int F() => 1; }
class C6 : I { public int F() => 1; }
class C7 : I { public int F() => 1; }

class Program
{
    static int Main()
    {
        I[] arr = new I[] { new C0(), new C1(), new C2(), new C3(), new C4(), new C5(), new C6(), new C7() };
        int s = 0;
        for (int it = 0; it < 100; it++)
            foreach (var i in arr) s += i.F();
        if (s != 800) return 1;
        Console.WriteLine("dispatch_stress: iface_loop_08_iter_100 ok");
        return 0;
    }
}
