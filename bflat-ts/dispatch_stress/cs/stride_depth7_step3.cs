// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

class S0 { public virtual int F() => 0; }
class S1 : S0 { public override int F() => base.F() + 3; }
class S2 : S1 { public override int F() => base.F() + 3; }
class S3 : S2 { public override int F() => base.F() + 3; }
class S4 : S3 { public override int F() => base.F() + 3; }
class S5 : S4 { public override int F() => base.F() + 3; }
class S6 : S5 { public override int F() => base.F() + 3; }
class S7 : S6 { public override int F() => base.F() + 3; }

class Program
{
    static int Main()
    {
        S0 s = new S7();
        if (s.F() != 21) return 1;
        Console.WriteLine("dispatch_stress: stride_depth7_step3 ok");
        return 0;
    }
}
