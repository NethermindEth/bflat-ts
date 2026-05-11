// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

struct Box<T> { public T Value; }
class Program
{
    static int Main()
    {
        var b = new Box<int> { Value = 7 };
        if (b.Value != 7) return 1;
        Console.WriteLine("generics: generic_struct_box ok");
        return 0;
    }
}
