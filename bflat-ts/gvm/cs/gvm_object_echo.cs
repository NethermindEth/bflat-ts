// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Base { public virtual object Echo<T>(T x) => x; }
class D : Base { public override object Echo<T>(T x) => x; }


class Program
{
    static int Main()
    {
        Base b = new D();
        object o = b.Echo<int>(7);
        if ((int)o != 7) return 1;
        Console.WriteLine("gvm: gvm_object_echo ok");
        return 0;
    }
}
