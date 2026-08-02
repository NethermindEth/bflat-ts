// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Base { public virtual List<T> Wrap<T>(T x) => new List<T> { x }; }
class D : Base { public override List<T> Wrap<T>(T x) => new List<T> { x, x }; }
class Program
{
    static int Main()
    {
        Base b = new D();
        var l = b.Wrap<long>(1L);
        if (l.Count != 2) return 1;
        Console.WriteLine("gvm: gvm_wrap_in_list_long ok");
        return 0;
    }
}
