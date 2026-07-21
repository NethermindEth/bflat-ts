// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
//
// Exercises: virtual dispatch through 5-level inheritance chain
// Bug targeted: vtable layout correctness for deep hierarchies in NativeAOT/RISC-V

using System;

class V0 { public virtual int Depth() => 0; public virtual string Tag() => "V0"; }
class V1 : V0 { public override int Depth() => 1; public override string Tag() => "V1"; }
class V2 : V1 { public override int Depth() => 2; public override string Tag() => "V2"; }
class V3 : V2 { public override int Depth() => 3; public override string Tag() => "V3"; }
class V4 : V3 { public override int Depth() => 4; public override string Tag() => "V4"; }

class Program
{
    static int Check(V0 obj, int expectedDepth, string expectedTag)
    {
        if (obj.Depth() != expectedDepth) return 1;
        if (obj.Tag() != expectedTag) return 1;
        return 0;
    }

    static int Main()
    {
        // All variables hold a V4 instance - dispatch must always reach V4 overrides
        V0 a0 = new V4();
        V1 a1 = new V4();
        V2 a2 = new V4();
        V3 a3 = new V4();
        V4 a4 = new V4();

        if (Check(a0, 4, "V4") != 0) return 1;
        if (Check(a1, 4, "V4") != 0) return 1;
        if (Check(a2, 4, "V4") != 0) return 1;
        if (Check(a3, 4, "V4") != 0) return 1;
        if (Check(a4, 4, "V4") != 0) return 1;

        // Mid-chain: V4 instance behind V2 reference
        V2 mid = new V4();
        if (mid.Depth() != 4) return 1;
        if (mid.Tag() != "V4") return 1;

        // Non-leaf instances: verify each level's own overrides
        V0 v0 = new V0();
        V0 v1 = new V1();
        V0 v2 = new V2();
        V0 v3 = new V3();

        if (Check(v0, 0, "V0") != 0) return 1;
        if (Check(v1, 1, "V1") != 0) return 1;
        if (Check(v2, 2, "V2") != 0) return 1;
        if (Check(v3, 3, "V3") != 0) return 1;

        // Array of mixed instances dispatched in loop
        V0[] objs = new V0[] { new V0(), new V1(), new V2(), new V3(), new V4() };
        for (int i = 0; i < objs.Length; i++)
        {
            if (objs[i].Depth() != i) return 1;
        }

        // Repeated dispatch in tight loop to stress vtable resolution
        int sum = 0;
        for (int iter = 0; iter < 200; iter++)
        {
            sum += a4.Depth();   // always 4
        }
        if (sum != 800) return 1;

        Console.WriteLine("virtual_deep: 5-level vtable dispatch ok");
        return 0;
    }
}