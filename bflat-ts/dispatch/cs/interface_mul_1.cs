// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface I { int F(int x); }
class C : I { public int F(int x) => x * 1; }


class Program
{
    static int Main()
    {
        I ii = new C();
        if (ii.F(3) != 3) return 1;
        Console.WriteLine("dispatch: interface_mul_1 ok");
        return 0;
    }
}
