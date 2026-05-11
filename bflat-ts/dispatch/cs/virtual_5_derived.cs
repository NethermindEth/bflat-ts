// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Base { public virtual int F() => 0; }
class D1 : Base { public override int F() => 1; }
class D2 : Base { public override int F() => 2; }
class D3 : Base { public override int F() => 3; }
class D4 : Base { public override int F() => 4; }
class D5 : Base { public override int F() => 5; }

class Program
{
    static int Main()
    {
        {Base b = new D1(); if (b.F() != 1) return 1;}
        {Base b = new D2(); if (b.F() != 2) return 1;}
        {Base b = new D3(); if (b.F() != 3) return 1;}
        {Base b = new D4(); if (b.F() != 4) return 1;}
        {Base b = new D5(); if (b.F() != 5) return 1;}
        Console.WriteLine("dispatch: virtual_5_derived ok");
        return 0;
    }
}
