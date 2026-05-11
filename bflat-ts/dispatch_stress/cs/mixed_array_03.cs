// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

abstract class A { public abstract int Code(); }
interface I { int Tag(); }
class M0 : A, I { public override int Code() => 0; public int Tag() => 0; }
class M1 : A, I { public override int Code() => 1; public int Tag() => 10; }
class M2 : A, I { public override int Code() => 2; public int Tag() => 20; }

class Program
{
    static int Main()
    {
        A[] arr = new A[] { new M0(), new M1(), new M2() };
        int s = 0;
        foreach (var a in arr) { s += a.Code(); if (a is I ii) s += ii.Tag(); }
        if (s != 33) return 1;
        Console.WriteLine("dispatch_stress: mixed_array_03 ok");
        return 0;
    }
}
