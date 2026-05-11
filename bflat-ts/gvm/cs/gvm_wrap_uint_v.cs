// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Base { public virtual List<T> M<T>(T x) => new List<T> { x }; }
class D : Base { public override List<T> M<T>(T x) => new List<T> { x, x }; }


class Program
{
    static int Main()
    {
        Base b = new D();
        var l = b.M<uint>(1u);
        if (l.Count != 2) return 1;
        Console.WriteLine("gvm: gvm_wrap_uint_v ok");
        return 0;
    }
}
