// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Base { public virtual T Echo<T>(T x) { return x; } }
class D : Base { public override T Echo<T>(T x) { return x; } }


class Program
{
    static int Main()
    {
        Base b = new D();
        var r = b.Echo<long>(42L);
        if (!r.Equals(42L)) return 1;
        Console.WriteLine("gvm: gvm_echo_long ok");
        return 0;
    }
}
