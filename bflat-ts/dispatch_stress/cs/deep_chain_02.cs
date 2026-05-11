// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class V0 { public virtual int F() => 0; }
class V1 : V0 { public override int F() => 1; }
class V2 : V1 { public override int F() => 2; }

class Program
{
    static int Main()
    {
        V0 v = new V2(); if (v.F() != 2) return 1;
        Console.WriteLine("dispatch_stress: deep_chain_02 ok");
        return 0;
    }
}
