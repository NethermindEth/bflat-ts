// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Base { public virtual int F() => 1; }
class D : Base { public override int F() => base.F() + 10; }


class Program
{
    static int Main()
    {
        Base b = new D();
        if (b.F() != 11) return 1;
        Console.WriteLine("dispatch: virtual_via_base ok");
        return 0;
    }
}
