// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Len<T>(T x) where T : class => x.ToString().Length;

    static int Main()
    {
        if (Len("abc") != 3) return 1;
        Console.WriteLine("generics: generic_method_class_constraint ok");
        return 0;
    }
}
