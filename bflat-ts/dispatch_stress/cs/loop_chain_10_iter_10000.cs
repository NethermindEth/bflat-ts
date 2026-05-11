// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class V0 { public virtual int F() => 0; }
class V1 : V0 { public override int F() => 1; }
class V2 : V1 { public override int F() => 2; }
class V3 : V2 { public override int F() => 3; }
class V4 : V3 { public override int F() => 4; }
class V5 : V4 { public override int F() => 5; }
class V6 : V5 { public override int F() => 6; }
class V7 : V6 { public override int F() => 7; }
class V8 : V7 { public override int F() => 8; }
class V9 : V8 { public override int F() => 9; }
class V10 : V9 { public override int F() => 10; }

class Program
{
    static int Main()
    {
        V0 v = new V10();
        int s = 0; for (int i = 0; i < 10000; i++) s += v.F();
        if (s != 100000) return 1;
        Console.WriteLine("dispatch_stress: loop_chain_10_iter_10000 ok");
        return 0;
    }
}
