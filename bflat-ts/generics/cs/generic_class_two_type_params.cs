// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Pair<A, B> { public A First; public B Second; }
class Program
{
    static int Main()
    {
        var p = new Pair<int, string> { First = 1, Second = "x" };
        if (p.First != 1 || p.Second != "x") return 1;
        Console.WriteLine("generics: generic_class_two_type_params ok");
        return 0;
    }
}
