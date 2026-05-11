// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface I { int F(int x); }
class C : I { public int F(int x) => x * 2; }


class Program
{
    static int Main()
    {
        I ii = new C();
        if (ii.F(3) != 6) return 1;
        Console.WriteLine("dispatch: interface_mul_2 ok");
        return 0;
    }
}
