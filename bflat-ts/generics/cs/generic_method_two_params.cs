// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static (A, B) Pair<A, B>(A a, B b) { return (a, b); }

    static int Main()
    {
        var p = Pair<int, string>(1, "x");
        if (p.Item1 != 1 || p.Item2 != "x") return 1;
        Console.WriteLine("generics: generic_method_two_params ok");
        return 0;
    }
}
