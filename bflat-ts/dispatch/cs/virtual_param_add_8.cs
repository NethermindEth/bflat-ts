// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Base { public virtual int F(int x) => x; }
class D : Base { public override int F(int x) => x + 8; }


class Program
{
    static int Main()
    {
        Base b = new D();
        if (b.F(5) != 13) return 1;
        Console.WriteLine("dispatch: virtual_param_add_8 ok");
        return 0;
    }
}
