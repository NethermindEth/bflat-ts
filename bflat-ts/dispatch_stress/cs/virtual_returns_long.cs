// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Base { public virtual long F() => 1L; }
class D : Base { public override long F() => 2L; }


class Program
{
    static int Main()
    {
        Base b = new D();
        if (b.F() != 2L) return 1;
        Console.WriteLine("dispatch_stress: virtual_returns_long ok");
        return 0;
    }
}
