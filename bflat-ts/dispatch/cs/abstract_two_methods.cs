// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

abstract class A {
    public abstract int F();
    public abstract int G();
}
class C : A {
    public override int F() => 1;
    public override int G() => 2;
}


class Program
{
    static int Main()
    {
        A a = new C();
        if (a.F() + a.G() != 3) return 1;
        Console.WriteLine("dispatch: abstract_two_methods ok");
        return 0;
    }
}
