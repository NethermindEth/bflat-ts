// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class A { public int F() => 1; }
class B : A { public new int F() => 2; }


class Program
{
    static int Main()
    {
        A a = new B();
        if (a.F() != 1) return 1;
        B b = new B();
        if (b.F() != 2) return 1;
        Console.WriteLine("dispatch: method_hiding_new ok");
        return 0;
    }
}
