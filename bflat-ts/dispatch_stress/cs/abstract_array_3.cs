// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

abstract class Base { public abstract int F(); }
class C0 : Base { public override int F() => 1; }
class C1 : Base { public override int F() => 2; }
class C2 : Base { public override int F() => 3; }

class Program
{
    static int Main()
    {
        Base[] arr = new Base[] { new C0(), new C1(), new C2() };
        int s = 0; foreach (var b in arr) s += b.F();
        if (s != 6) return 1;
        Console.WriteLine("dispatch_stress: abstract_array_3 ok");
        return 0;
    }
}
