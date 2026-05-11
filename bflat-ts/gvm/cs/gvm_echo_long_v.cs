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
        if (!b.M<long>(1L).Equals(1L)) return 1;
        Console.WriteLine("gvm: gvm_echo_long_v ok");
        return 0;
    }
}
