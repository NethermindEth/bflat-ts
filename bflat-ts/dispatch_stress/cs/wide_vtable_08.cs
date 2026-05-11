// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class B {
    public virtual int F0() => 0;
    public virtual int F1() => 1;
    public virtual int F2() => 2;
    public virtual int F3() => 3;
    public virtual int F4() => 4;
    public virtual int F5() => 5;
    public virtual int F6() => 6;
    public virtual int F7() => 7;
}
class D : B {
    public override int F0() => 100;
    public override int F1() => 101;
    public override int F2() => 102;
    public override int F3() => 103;
    public override int F4() => 104;
    public override int F5() => 105;
    public override int F6() => 106;
    public override int F7() => 107;
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
        s += b.F4();
        s += b.F5();
        s += b.F6();
        s += b.F7();
        if (s != 828) return 1;
        Console.WriteLine("dispatch_stress: wide_vtable_08 ok");
        return 0;
    }
}
