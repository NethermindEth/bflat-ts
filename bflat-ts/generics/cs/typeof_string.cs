// Copyright (C) 2025-2026 Demerzel Solutions Limited (Nethermind)
using System;
using System.Collections.Generic;

class Program
{
    static int Main()
    {
        if (typeof(string).Name != "String") return 1;
        Console.WriteLine("generics: typeof_string ok");
        return 0;
    }
}
