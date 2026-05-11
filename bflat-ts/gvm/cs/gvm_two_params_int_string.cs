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
        var p = b.Pair<int, string>(1, "a");
        if (!p.Item1.Equals(1)) return 1;
        if (!p.Item2.Equals("a")) return 1;
        Console.WriteLine("gvm: gvm_two_params_int_string ok");
        return 0;
    }
}
