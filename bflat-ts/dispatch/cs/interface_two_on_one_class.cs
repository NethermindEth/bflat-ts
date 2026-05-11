// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface IA { int A(); }
interface IB { int B(); }
class C : IA, IB { public int A() => 1; public int B() => 2; }


class Program
{
    static int Main()
    {
        IA a = new C(); IB b = new C();
        if (a.A() + b.B() != 3) return 1;
        Console.WriteLine("dispatch: interface_two_on_one_class ok");
        return 0;
    }
}
