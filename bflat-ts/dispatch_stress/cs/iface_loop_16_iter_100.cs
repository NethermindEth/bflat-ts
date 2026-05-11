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
class C8 : I { public int F() => 1; }
class C9 : I { public int F() => 1; }
class C10 : I { public int F() => 1; }
class C11 : I { public int F() => 1; }
class C12 : I { public int F() => 1; }
class C13 : I { public int F() => 1; }
class C14 : I { public int F() => 1; }
class C15 : I { public int F() => 1; }

class Program
{
    static int Main()
    {
        I[] arr = new I[] { new C0(), new C1(), new C2(), new C3(), new C4(), new C5(), new C6(), new C7(), new C8(), new C9(), new C10(), new C11(), new C12(), new C13(), new C14(), new C15() };
        int s = 0;
        for (int it = 0; it < 100; it++)
            foreach (var i in arr) s += i.F();
        if (s != 1600) return 1;
        Console.WriteLine("dispatch_stress: iface_loop_16_iter_100 ok");
        return 0;
    }
}
