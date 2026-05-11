// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static B Apply<A, B>(A x, Func<A, B> f) => f(x);
    static int Main()
    {
        if (Apply<int, int>(3, n => n * 2) != 6) return 1;
        Console.WriteLine("generics: generic_func_two ok");
        return 0;
    }
}
