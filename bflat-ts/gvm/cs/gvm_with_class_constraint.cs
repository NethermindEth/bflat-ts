// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Base { public virtual T Null<T>() where T : class => null; }
class D : Base { public override T Null<T>() => null; }


class Program
{
    static int Main()
    {
        Base b = new D();
        if (b.Null<string>() != null) return 1;
        Console.WriteLine("gvm: gvm_with_class_constraint ok");
        return 0;
    }
}
