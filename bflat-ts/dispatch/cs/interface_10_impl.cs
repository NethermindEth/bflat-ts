// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface I { int F(); }
class C1 : I { public int F() => 1; }
class C2 : I { public int F() => 2; }
class C3 : I { public int F() => 3; }
class C4 : I { public int F() => 4; }
class C5 : I { public int F() => 5; }
class C6 : I { public int F() => 6; }
class C7 : I { public int F() => 7; }
class C8 : I { public int F() => 8; }
class C9 : I { public int F() => 9; }
class C10 : I { public int F() => 10; }

class Program
{
    static int Main()
    {
        {I i = new C1(); if (i.F() != 1) return 1;}
        {I i = new C2(); if (i.F() != 2) return 1;}
        {I i = new C3(); if (i.F() != 3) return 1;}
        {I i = new C4(); if (i.F() != 4) return 1;}
        {I i = new C5(); if (i.F() != 5) return 1;}
        {I i = new C6(); if (i.F() != 6) return 1;}
        {I i = new C7(); if (i.F() != 7) return 1;}
        {I i = new C8(); if (i.F() != 8) return 1;}
        {I i = new C9(); if (i.F() != 9) return 1;}
        {I i = new C10(); if (i.F() != 10) return 1;}
        Console.WriteLine("dispatch: interface_10_impl ok");
        return 0;
    }
}
