// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class C0 { public virtual T Echo<T>(T x) => x; }
class C1 : C0 { public override T Echo<T>(T x) => x; }
class C2 : C1 { public override T Echo<T>(T x) => x; }

class Program
{
    static int Main()
    {
        C0 c = new C2();
        if (c.Echo<int>(7) != 7) return 1;
        if (c.Echo<string>("hi") != "hi") return 1;
        Console.WriteLine("gvm: gvm_chain_2 ok");
        return 0;
    }
}
