// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class A { public virtual int F() => 10; }
class B : A { public override int F() => 13; }
class C : B { public override int F() => base.F() + 100; }


class Program
{
    static int Main()
    {
        A a = new C(); if (a.F() != 113) return 1;
        Console.WriteLine("dispatch: chain_base_add_13 ok");
        return 0;
    }
}
