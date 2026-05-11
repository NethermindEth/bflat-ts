// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface I { int F(); }
class A : I { public int F() => 1; }
class B : I { public int F() => 2; }


class Program
{
    static int Main()
    {
        I[] arr = { new A(), new B(), new B() };
        int s = 0; foreach (var i in arr) s += i.F();
        if (s != 5) return 1;
        Console.WriteLine("dispatch: poly_array_interface ok");
        return 0;
    }
}
