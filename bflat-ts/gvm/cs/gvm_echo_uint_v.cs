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
        if (!b.M<uint>(1u).Equals(1u)) return 1;
        Console.WriteLine("gvm: gvm_echo_uint_v ok");
        return 0;
    }
}
