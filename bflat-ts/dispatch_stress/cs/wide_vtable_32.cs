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
    public virtual int F16() => 16;
    public virtual int F17() => 17;
    public virtual int F18() => 18;
    public virtual int F19() => 19;
    public virtual int F20() => 20;
    public virtual int F21() => 21;
    public virtual int F22() => 22;
    public virtual int F23() => 23;
    public virtual int F24() => 24;
    public virtual int F25() => 25;
    public virtual int F26() => 26;
    public virtual int F27() => 27;
    public virtual int F28() => 28;
    public virtual int F29() => 29;
    public virtual int F30() => 30;
    public virtual int F31() => 31;
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
    public override int F16() => 116;
    public override int F17() => 117;
    public override int F18() => 118;
    public override int F19() => 119;
    public override int F20() => 120;
    public override int F21() => 121;
    public override int F22() => 122;
    public override int F23() => 123;
    public override int F24() => 124;
    public override int F25() => 125;
    public override int F26() => 126;
    public override int F27() => 127;
    public override int F28() => 128;
    public override int F29() => 129;
    public override int F30() => 130;
    public override int F31() => 131;
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
        s += b.F16();
        s += b.F17();
        s += b.F18();
        s += b.F19();
        s += b.F20();
        s += b.F21();
        s += b.F22();
        s += b.F23();
        s += b.F24();
        s += b.F25();
        s += b.F26();
        s += b.F27();
        s += b.F28();
        s += b.F29();
        s += b.F30();
        s += b.F31();
        if (s != 3696) return 1;
        Console.WriteLine("dispatch_stress: wide_vtable_32 ok");
        return 0;
    }
}
