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
    public virtual int F8() => 8;
    public virtual int F9() => 9;
    public virtual int F10() => 10;
    public virtual int F11() => 11;
    public virtual int F12() => 12;
    public virtual int F13() => 13;
    public virtual int F14() => 14;
    public virtual int F15() => 15;
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
    public override int F8() => 108;
    public override int F9() => 109;
    public override int F10() => 110;
    public override int F11() => 111;
    public override int F12() => 112;
    public override int F13() => 113;
    public override int F14() => 114;
    public override int F15() => 115;
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
        s += b.F8();
        s += b.F9();
        s += b.F10();
        s += b.F11();
        s += b.F12();
        s += b.F13();
        s += b.F14();
        s += b.F15();
        if (s != 1720) return 1;
        Console.WriteLine("dispatch_stress: wide_vtable_16 ok");
        return 0;
    }
}
