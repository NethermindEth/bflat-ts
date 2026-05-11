// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Base { public virtual string F() => "a"; }
class D : Base { public override string F() => "b"; }


class Program
{
    static int Main()
    {
        Base b = new D();
        if (b.F() != "b") return 1;
        Console.WriteLine("dispatch_stress: virtual_returns_string ok");
        return 0;
    }
}
