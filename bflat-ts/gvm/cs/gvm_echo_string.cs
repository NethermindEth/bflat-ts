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
        var r = b.Echo<string>("hi");
        if (!r.Equals("hi")) return 1;
        Console.WriteLine("gvm: gvm_echo_string ok");
        return 0;
    }
}
