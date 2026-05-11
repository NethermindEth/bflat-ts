// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Base { public virtual int X => 1; }
class D : Base { public override int X => 2; }


class Program
{
    static int Main()
    {
        Base b = new D();
        if (b.X != 2) return 1;
        Console.WriteLine("dispatch: virtual_property ok");
        return 0;
    }
}
