// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Base { public virtual T Echo<T>(T x) => x; }
class D : Base { public override T Echo<T>(T x) => x; }


class Program
{
    static int Main()
    {
        Base b = new D();
        if (b.Echo<int>(0) != 0) return 1;
        if (b.Echo<int>(1) != 1) return 1;
        if (b.Echo<int>(2) != 2) return 1;
        Console.WriteLine("gvm: gvm_many_calls_int_3 ok");
        return 0;
    }
}
