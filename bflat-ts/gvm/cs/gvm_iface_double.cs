// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

interface I { T Pick<T>(T x); }
class C : I { public T Pick<T>(T x) => x; }


class Program
{
    static int Main()
    {
        I i = new C();
        var r = i.Pick<double>(1.5);
        if (!r.Equals(1.5)) return 1;
        Console.WriteLine("gvm: gvm_iface_double ok");
        return 0;
    }
}
