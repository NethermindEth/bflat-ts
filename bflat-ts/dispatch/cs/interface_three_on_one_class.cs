// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;

interface I1 { int F1(); }
interface I2 { int F2(); }
interface I3 { int F3(); }
class C : I1, I2, I3 { public int F1() => 1; public int F2() => 2; public int F3() => 3; }


class Program
{
    static int Main()
    {
        var c = new C();
        if (((I1)c).F1() + ((I2)c).F2() + ((I3)c).F3() != 6) return 1;
        Console.WriteLine("dispatch: interface_three_on_one_class ok");
        return 0;
    }
}
