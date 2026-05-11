// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Foo { public int X = 5; }
class Program
{
    static T Make<T>() where T : class, new() => new T();
    static int Main()
    {
        var f = Make<Foo>();
        if (f.X != 5) return 1;
        Console.WriteLine("generics: constraint_class_new ok");
        return 0;
    }
}
