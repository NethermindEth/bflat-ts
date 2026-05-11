// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Base { public virtual int F(int i) => i; }
class D : Base { public override int F(int i) => i + 1; }


class Program
{
    static int Main()
    {
        Base b = new D(); int s = 0;
        for (int i = 0; i < 1000; i++) s += b.F(i);
        if (s != 500500) return 1;
        Console.WriteLine("dispatch: virtual_loop_1000 ok");
        return 0;
    }
}
