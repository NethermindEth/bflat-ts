// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

abstract class A { public abstract int F(); }
class C : A { public override int F() => 5; }


class Program
{
    static int Main()
    {
        A a = new C();
        if (a.F() != 5) return 1;
        Console.WriteLine("dispatch: abstract_one_method ok");
        return 0;
    }
}
