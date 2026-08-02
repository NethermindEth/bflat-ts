// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Base { public virtual double F() => 1.5; }
class D : Base { public override double F() => 2.5; }


class Program
{
    static int Main()
    {
        Base b = new D();
        if (b.F() != 2.5) return 1;
        Console.WriteLine("dispatch_stress: virtual_returns_double ok");
        return 0;
    }
}
