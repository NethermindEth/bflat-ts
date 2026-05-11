// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Base { public virtual int F() => 0; }
class D1 : Base { public override int F() => 1; }
class D2 : Base { public override int F() => 2; }

class Program
{
    static int Main()
    {
        {Base b = new D1(); if (b.F() != 1) return 1;}
        {Base b = new D2(); if (b.F() != 2) return 1;}
        Console.WriteLine("dispatch: virtual_2_derived ok");
        return 0;
    }
}
