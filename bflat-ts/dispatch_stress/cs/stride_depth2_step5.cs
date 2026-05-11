// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class S0 { public virtual int F() => 0; }
class S1 : S0 { public override int F() => base.F() + 5; }
class S2 : S1 { public override int F() => base.F() + 5; }

class Program
{
    static int Main()
    {
        S0 s = new S2();
        if (s.F() != 10) return 1;
        Console.WriteLine("dispatch_stress: stride_depth2_step5 ok");
        return 0;
    }
}
