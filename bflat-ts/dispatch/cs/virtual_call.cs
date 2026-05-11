// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Base { public virtual string Name => "B"; }
class D : Base { public override string Name => "D"; }


class Program
{
    static int Main()
    {
        Base b = new D();
        if (b.Name != "D") return 1;
        Console.WriteLine("dispatch: virtual_call ok");
        return 0;
    }
}
