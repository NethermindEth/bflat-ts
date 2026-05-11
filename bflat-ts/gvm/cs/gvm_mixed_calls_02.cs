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
        Console.WriteLine("gvm: gvm_mixed_calls_02 ok");
        return 0;
    }
}
