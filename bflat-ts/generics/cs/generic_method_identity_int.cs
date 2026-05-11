// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static T Id<T>(T x) { return x; }

    static int Main()
    {
        if (Id<int>(42) != 42) return 1;
        Console.WriteLine("generics: generic_method_identity_int ok");
        return 0;
    }
}
