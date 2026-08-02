// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static T Zero<T>() where T : struct => default(T);

    static int Main()
    {
        if (Zero<int>() != 0) return 1;
        Console.WriteLine("generics: generic_method_struct_constraint ok");
        return 0;
    }
}
