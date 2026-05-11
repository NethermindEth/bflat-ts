// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Base { public virtual T Zero<T>() where T : struct => default(T); }
class D : Base { public override T Zero<T>() => default(T); }


class Program
{
    static int Main()
    {
        Base b = new D();
        if (b.Zero<int>() != 0) return 1;
        Console.WriteLine("gvm: gvm_with_struct_constraint ok");
        return 0;
    }
}
