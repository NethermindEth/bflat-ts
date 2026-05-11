// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        if (typeof(int).Name != "Int32") return 1;
        Console.WriteLine("generics: typeof_int ok");
        return 0;
    }
}
