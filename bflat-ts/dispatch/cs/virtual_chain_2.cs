// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class C0 { public virtual int F() => 0; }
class C1 : C0 { public override int F() => 1; }
class C2 : C1 { public override int F() => 2; }

class Program
{
    static int Main()
    {
        C0 c = new C2(); if (c.F() != 2) return 1;
        Console.WriteLine("dispatch: virtual_chain_2 ok");
        return 0;
    }
}
