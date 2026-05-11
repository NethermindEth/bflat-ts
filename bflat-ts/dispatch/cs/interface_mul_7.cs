// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface I { int F(int x); }
class C : I { public int F(int x) => x * 7; }


class Program
{
    static int Main()
    {
        I ii = new C();
        if (ii.F(3) != 21) return 1;
        Console.WriteLine("dispatch: interface_mul_7 ok");
        return 0;
    }
}
