// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

interface IHasX { int X { get; } }
class C : IHasX { public int X => 7; }
class Program
{
    static int GetX<T>(T t) where T : IHasX => t.X;
    static int Main()
    {
        var c = new C();
        if (GetX(c) != 7) return 1;
        Console.WriteLine("generics: constraint_interface ok");
        return 0;
    }
}
