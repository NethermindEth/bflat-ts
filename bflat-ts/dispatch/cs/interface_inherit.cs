// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface IBase { int F(); }
interface IDerived : IBase { int G(); }
class C : IDerived { public int F() => 1; public int G() => 2; }


class Program
{
    static int Main()
    {
        IDerived d = new C();
        if (d.F() + d.G() != 3) return 1;
        Console.WriteLine("dispatch: interface_inherit ok");
        return 0;
    }
}
