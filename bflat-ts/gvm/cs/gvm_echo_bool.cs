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
        var r = b.Echo<bool>(true);
        if (!r.Equals(true)) return 1;
        Console.WriteLine("gvm: gvm_echo_bool ok");
        return 0;
    }
}
