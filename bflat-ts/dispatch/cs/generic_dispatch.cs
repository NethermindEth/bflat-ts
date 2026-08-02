// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface I { int F(); }
class C : I { public int F() => 7; }
class Caller<T> where T : I { public int Do(T t) => t.F(); }


class Program
{
    static int Main()
    {
        var c = new Caller<C>();
        if (c.Do(new C()) != 7) return 1;
        Console.WriteLine("dispatch: generic_dispatch ok");
        return 0;
    }
}
