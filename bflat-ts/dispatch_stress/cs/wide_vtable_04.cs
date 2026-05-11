// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class B {
    public virtual int F0() => 0;
    public virtual int F1() => 1;
    public virtual int F2() => 2;
    public virtual int F3() => 3;
}
class D : B {
    public override int F0() => 100;
    public override int F1() => 101;
    public override int F2() => 102;
    public override int F3() => 103;
}


class Program
{
    static int Main()
    {
        B b = new D();
        int s = 0;
        s += b.F0();
        s += b.F1();
        s += b.F2();
        s += b.F3();
        if (s != 406) return 1;
        Console.WriteLine("dispatch_stress: wide_vtable_04 ok");
        return 0;
    }
}
