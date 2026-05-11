// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Box<T> { public T Value; }
class Program
{
    static int Main()
    {
        var b = new Box<string> { Value = "hi" };
        if (b.Value != "hi") return 1;
        Console.WriteLine("generics: generic_class_box_string ok");
        return 0;
    }
}
