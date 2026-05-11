// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

abstract class Base { public abstract int F(); }
class C0 : Base { public override int F() => 1; }
class C1 : Base { public override int F() => 2; }
class C2 : Base { public override int F() => 3; }
class C3 : Base { public override int F() => 4; }
class C4 : Base { public override int F() => 5; }
class C5 : Base { public override int F() => 6; }
class C6 : Base { public override int F() => 7; }
class C7 : Base { public override int F() => 8; }

class Program
{
    static int Main()
    {
        Base[] arr = new Base[] { new C0(), new C1(), new C2(), new C3(), new C4(), new C5(), new C6(), new C7() };
        int s = 0; foreach (var b in arr) s += b.F();
        if (s != 36) return 1;
        Console.WriteLine("dispatch_stress: abstract_array_8 ok");
        return 0;
    }
}
