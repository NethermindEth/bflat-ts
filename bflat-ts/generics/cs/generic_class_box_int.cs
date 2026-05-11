// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Box<T> { public T Value; }
class Program
{
    static int Main()
    {
        var b = new Box<int> { Value = 42 };
        if (b.Value != 42) return 1;
        Console.WriteLine("generics: generic_class_box_int ok");
        return 0;
    }
}
