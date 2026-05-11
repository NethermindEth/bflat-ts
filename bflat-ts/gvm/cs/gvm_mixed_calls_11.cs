// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Base { public virtual T M<T>(T x) => x; }
class D : Base { public override T M<T>(T x) => x; }


class Program
{
    static int Main()
    {
        Base b = new D();
        if (b.M<int>(0) != 0) return 1;
        if (b.M<string>("s0") != "s0") return 1;
        if (b.M<int>(1) != 1) return 1;
        if (b.M<string>("s1") != "s1") return 1;
        if (b.M<int>(2) != 2) return 1;
        if (b.M<string>("s2") != "s2") return 1;
        if (b.M<int>(3) != 3) return 1;
        if (b.M<string>("s3") != "s3") return 1;
        if (b.M<int>(4) != 4) return 1;
        if (b.M<string>("s4") != "s4") return 1;
        if (b.M<int>(5) != 5) return 1;
        if (b.M<string>("s5") != "s5") return 1;
        if (b.M<int>(6) != 6) return 1;
        if (b.M<string>("s6") != "s6") return 1;
        if (b.M<int>(7) != 7) return 1;
        if (b.M<string>("s7") != "s7") return 1;
        if (b.M<int>(8) != 8) return 1;
        if (b.M<string>("s8") != "s8") return 1;
        if (b.M<int>(9) != 9) return 1;
        if (b.M<string>("s9") != "s9") return 1;
        if (b.M<int>(10) != 10) return 1;
        if (b.M<string>("s10") != "s10") return 1;
        Console.WriteLine("gvm: gvm_mixed_calls_11 ok");
        return 0;
    }
}
