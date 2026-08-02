// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Helper { public static T Echo<T>(T x) => x; }
class Base { public virtual T Run<T>(T x) => Helper.Echo<T>(x); }
class D : Base { public override T Run<T>(T x) => Helper.Echo<T>(x); }
class Program
{
    static int Main()
    {
        Base b = new D();
        if (b.Run<int>(7) != 7) return 1;
        if (b.Run<string>("a") != "a") return 1;
        Console.WriteLine("gvm: gvm_calls_helper ok");
        return 0;
    }
}
