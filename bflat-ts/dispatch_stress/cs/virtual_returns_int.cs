// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Base { public virtual int F() => 1; }
class D : Base { public override int F() => 2; }


class Program
{
    static int Main()
    {
        Base b = new D();
        if (b.F() != 2) return 1;
        Console.WriteLine("dispatch_stress: virtual_returns_int ok");
        return 0;
    }
}
