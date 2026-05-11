// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class A { public virtual int F() => 1; }
class B : A { public sealed override int F() => 2; }


class Program
{
    static int Main()
    {
        A a = new B();
        if (a.F() != 2) return 1;
        Console.WriteLine("dispatch: sealed_override ok");
        return 0;
    }
}
