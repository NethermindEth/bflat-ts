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
        if (!b.M<float>(1f).Equals(1f)) return 1;
        Console.WriteLine("gvm: gvm_echo_float_v ok");
        return 0;
    }
}
