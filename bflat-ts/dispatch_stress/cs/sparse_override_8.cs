// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class C0 { public virtual int F() => 0; }
class C1 : C0 { }
class C2 : C1 { public override int F() => 2; }
class C3 : C2 { }
class C4 : C3 { public override int F() => 4; }
class C5 : C4 { }
class C6 : C5 { public override int F() => 6; }
class C7 : C6 { }
class C8 : C7 { public override int F() => 8; }

class Program
{
    static int Main()
    {
        C0 c = new C8();
        if (c.F() != 8) return 1;
        Console.WriteLine("dispatch_stress: sparse_override_8 ok");
        return 0;
    }
}
