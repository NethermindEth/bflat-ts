// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class C0 { public virtual int F() => 0; }
class C1 : C0 { public override int F() => 1; }
class C2 : C1 { public override int F() => 2; }
class C3 : C2 { public override int F() => 3; }
class C4 : C3 { public override int F() => 4; }

class Program
{
    static int Main()
    {
        C0 c = new C4(); if (c.F() != 4) return 1;
        Console.WriteLine("dispatch: virtual_chain_4 ok");
        return 0;
    }
}
