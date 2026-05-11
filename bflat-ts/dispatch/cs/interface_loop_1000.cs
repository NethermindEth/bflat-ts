// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface I { int F(int i); }
class C : I { public int F(int i) => i + 1; }


class Program
{
    static int Main()
    {
        I i = new C(); int s = 0;
        for (int k = 0; k < 1000; k++) s += i.F(k);
        if (s != 500500) return 1;
        Console.WriteLine("dispatch: interface_loop_1000 ok");
        return 0;
    }
}
