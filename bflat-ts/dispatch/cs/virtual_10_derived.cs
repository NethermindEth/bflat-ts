// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class Base { public virtual int F() => 0; }
class D1 : Base { public override int F() => 1; }
class D2 : Base { public override int F() => 2; }
class D3 : Base { public override int F() => 3; }
class D4 : Base { public override int F() => 4; }
class D5 : Base { public override int F() => 5; }
class D6 : Base { public override int F() => 6; }
class D7 : Base { public override int F() => 7; }
class D8 : Base { public override int F() => 8; }
class D9 : Base { public override int F() => 9; }
class D10 : Base { public override int F() => 10; }

class Program
{
    static int Main()
    {
        {Base b = new D1(); if (b.F() != 1) return 1;}
        {Base b = new D2(); if (b.F() != 2) return 1;}
        {Base b = new D3(); if (b.F() != 3) return 1;}
        {Base b = new D4(); if (b.F() != 4) return 1;}
        {Base b = new D5(); if (b.F() != 5) return 1;}
        {Base b = new D6(); if (b.F() != 6) return 1;}
        {Base b = new D7(); if (b.F() != 7) return 1;}
        {Base b = new D8(); if (b.F() != 8) return 1;}
        {Base b = new D9(); if (b.F() != 9) return 1;}
        {Base b = new D10(); if (b.F() != 10) return 1;}
        Console.WriteLine("dispatch: virtual_10_derived ok");
        return 0;
    }
}
