// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface I { int F(); }
class C0 : I { public int F() => 0; }
class C1 : I { public int F() => 1; }
class C2 : I { public int F() => 2; }
class C3 : I { public int F() => 3; }
class C4 : I { public int F() => 4; }
class C5 : I { public int F() => 5; }
class C6 : I { public int F() => 6; }
class C7 : I { public int F() => 7; }

class Program
{
    static int Main()
    {
        I[] arr = new I[] { new C0(), new C1(), new C2(), new C3(), new C4(), new C5(), new C6(), new C7() };
        int s = 0; foreach (var i in arr) s += i.F();
        if (s != 28) return 1;
        Console.WriteLine("dispatch_stress: wide_iface_08 ok");
        return 0;
    }
}
