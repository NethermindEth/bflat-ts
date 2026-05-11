// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class A { public virtual int F() => 10; }
class B : A { public override int F() => 8; }
class C : B { public override int F() => base.F() + 100; }


class Program
{
    static int Main()
    {
        A a = new C(); if (a.F() != 108) return 1;
        Console.WriteLine("dispatch: chain_base_add_8 ok");
        return 0;
    }
}
