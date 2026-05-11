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
        var p = b.Pair<string, int>("x", 5);
        if (!p.Item1.Equals("x")) return 1;
        if (!p.Item2.Equals(5)) return 1;
        Console.WriteLine("gvm: gvm_two_params_string_int ok");
        return 0;
    }
}
