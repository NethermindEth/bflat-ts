// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Base { public virtual int F() => 0; }
class A : Base { public override int F() => 1; }
class B : Base { public override int F() => 2; }


class Program
{
    static int Main()
    {
        Base[] arr = { new A(), new B(), new A() };
        int s = 0; foreach (var b in arr) s += b.F();
        if (s != 4) return 1;
        Console.WriteLine("dispatch: poly_array_virtual ok");
        return 0;
    }
}
