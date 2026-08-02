// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Base { public virtual (A, B) Pair<A, B>(A a, B b) => (a, b); }
class D : Base { public override (A, B) Pair<A, B>(A a, B b) => (a, b); }


class Program
{
    static int Main()
    {
        Base b = new D();
        var p = b.Pair<long, int>(1L, 2);
        if (!p.Item1.Equals(1L)) return 1;
        if (!p.Item2.Equals(2)) return 1;
        Console.WriteLine("gvm: gvm_two_params_long_int ok");
        return 0;
    }
}
