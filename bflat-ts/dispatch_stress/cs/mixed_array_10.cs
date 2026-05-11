// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

abstract class A { public abstract int Code(); }
interface I { int Tag(); }
class M0 : A, I { public override int Code() => 0; public int Tag() => 0; }
class M1 : A, I { public override int Code() => 1; public int Tag() => 10; }
class M2 : A, I { public override int Code() => 2; public int Tag() => 20; }
class M3 : A, I { public override int Code() => 3; public int Tag() => 30; }
class M4 : A, I { public override int Code() => 4; public int Tag() => 40; }
class M5 : A, I { public override int Code() => 5; public int Tag() => 50; }
class M6 : A, I { public override int Code() => 6; public int Tag() => 60; }
class M7 : A, I { public override int Code() => 7; public int Tag() => 70; }
class M8 : A, I { public override int Code() => 8; public int Tag() => 80; }
class M9 : A, I { public override int Code() => 9; public int Tag() => 90; }

class Program
{
    static int Main()
    {
        A[] arr = new A[] { new M0(), new M1(), new M2(), new M3(), new M4(), new M5(), new M6(), new M7(), new M8(), new M9() };
        int s = 0;
        foreach (var a in arr) { s += a.Code(); if (a is I ii) s += ii.Tag(); }
        if (s != 495) return 1;
        Console.WriteLine("dispatch_stress: mixed_array_10 ok");
        return 0;
    }
}
