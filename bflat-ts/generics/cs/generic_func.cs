// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static T Apply<T>(Func<T> f) => f();
    static int Main()
    {
        if (Apply<int>(() => 7) != 7) return 1;
        Console.WriteLine("generics: generic_func ok");
        return 0;
    }
}
