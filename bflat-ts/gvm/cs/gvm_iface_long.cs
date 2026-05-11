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
        var r = i.Pick<long>(1L);
        if (!r.Equals(1L)) return 1;
        Console.WriteLine("gvm: gvm_iface_long ok");
        return 0;
    }
}
