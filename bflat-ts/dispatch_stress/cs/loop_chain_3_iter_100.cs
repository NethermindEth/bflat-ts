// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class V0 { public virtual int F() => 0; }
class V1 : V0 { public override int F() => 1; }
class V2 : V1 { public override int F() => 2; }
class V3 : V2 { public override int F() => 3; }

class Program
{
    static int Main()
    {
        V0 v = new V3();
        int s = 0; for (int i = 0; i < 100; i++) s += v.F();
        if (s != 300) return 1;
        Console.WriteLine("dispatch_stress: loop_chain_3_iter_100 ok");
        return 0;
    }
}
